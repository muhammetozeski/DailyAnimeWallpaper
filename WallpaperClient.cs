using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text.Json;
using Polly.Timeout;

namespace DailyAnimeWallpaper;

/// <summary>Holds a validated wallpaper binary payload and its source metadata.</summary>
/// <param name="Bytes">The verified image file bytes.</param>
/// <param name="Extension">The canonical file extension including leading dot.</param>
/// <param name="SourceUrl">The resolved image or endpoint URL.</param>
/// <param name="ProviderName">The provider display name.</param>
/// <param name="SubcategoryName">The subcategory display name.</param>
internal sealed record DownloadedWallpaper(
    byte[] Bytes,
    string Extension,
    string SourceUrl,
    string ProviderName,
    string SubcategoryName);

/// <summary>Fetches metadata and binary wallpaper images from built-in and user-defined anime APIs.</summary>
internal static class WallpaperClient
{
    const bool IsLogEnabled = true;
    static readonly string[] RejectedExtensions =
        [".mp4", ".webm", ".mkv", ".mov", ".avi", ".swf", ".zip", ".rar", ".7z", ".mp3", ".ogg", ".wav"];
    static readonly string[] BooruUrlFields = ["file_url", "large_file_url", "jpeg_url", "sample_url"];
    static readonly HttpClient Client = CreateHttpClient();

    /// <summary>Resolves and downloads a validated anime image from the specified provider and subcategory.</summary>
    /// <param name="provider">The selected anime wallpaper provider.</param>
    /// <param name="subcategory">The selected subcategory within <paramref name="provider"/>.</param>
    /// <param name="cancellationToken">Cooperative cancellation token.</param>
    /// <returns>The verified image bytes, detected extension, and resolved URL.</returns>
    public static async Task<DownloadedWallpaper> FetchWallpaperAsync(
        WallpaperProvider provider,
        WallpaperSubcategory subcategory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(subcategory);

        string endpoint = PickCandidateEndpoint(subcategory.Endpoint);
        Log($"Fetching anime wallpaper; provider={provider.Name}; subcategory={subcategory.Name}; mode={provider.ResponseMode}; endpoint={endpoint}", isRun: IsLogEnabled);

        if (provider.ResponseMode == ProviderResponseMode.DirectUrlOrImage)
            return await FetchDirectModeAsync(provider, subcategory, endpoint, cancellationToken).ConfigureAwait(false);

        string imageUrl = provider.ResponseMode switch
        {
            ProviderResponseMode.JsonElementPath => await ResolveJsonPathUrlAsync(provider, endpoint, cancellationToken).ConfigureAwait(false),
            ProviderResponseMode.NekosMoeId => await ResolveNekosMoeUrlAsync(endpoint, cancellationToken).ConfigureAwait(false),
            ProviderResponseMode.BooruPostList => await ResolveBooruPostUrlAsync(endpoint, cancellationToken).ConfigureAwait(false),
            ProviderResponseMode.GelbooruDapi => await ResolveGelbooruDapiUrlAsync(provider, subcategory, endpoint, cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException($"Unsupported response mode: {provider.ResponseMode}")
        };

        (byte[] imageBytes, string finalUrl) = await DownloadBytesAsync(imageUrl, endpoint, cancellationToken).ConfigureAwait(false);
        string? extension = DetectImageExtension(imageBytes);
        if (extension is null)
            throw new InvalidDataException($"Downloaded content from '{finalUrl}' is not a valid image ({imageBytes.Length} bytes).");

        return new DownloadedWallpaper(imageBytes, extension, finalUrl, provider.Name, subcategory.Name);
    }

    /// <summary>Normalizes raw URL strings from JSON or plain-text responses into valid HTTP/HTTPS URLs.</summary>
    /// <param name="rawUrl">The raw URL text.</param>
    /// <param name="preferStaticOnly">When true, rejects animated GIF and video extensions.</param>
    /// <returns>A normalized absolute URL, or null when invalid or rejected.</returns>
    public static string? NormalizeImageUrl(string? rawUrl, bool preferStaticOnly = false)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            return null;

        string trimmed = rawUrl.Trim().Trim('"');
        if (trimmed.Length == 0)
            return null;

        if (trimmed.StartsWith("//", StringComparison.Ordinal))
            trimmed = "https:" + trimmed;
        else if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                 !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            int slashIndex = trimmed.IndexOf('/');
            int dotIndex = trimmed.IndexOf('.');
            if (dotIndex > 0 && (slashIndex < 0 || dotIndex < slashIndex))
                trimmed = "https://" + trimmed;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri))
            return null;

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
            return null;

        string ext = Path.GetExtension(uri.AbsolutePath);
        if (!string.IsNullOrEmpty(ext))
        {
            foreach (string rejected in RejectedExtensions)
            {
                if (ext.Equals(rejected, StringComparison.OrdinalIgnoreCase))
                    return null;
            }

            if (preferStaticOnly && ext.Equals(".gif", StringComparison.OrdinalIgnoreCase))
                return null;
        }

        return uri.AbsoluteUri;
    }

    /// <summary>Extracts a valid image URL from a JSON document using dot and bracket notation.</summary>
    /// <param name="json">The raw JSON string.</param>
    /// <param name="path">Element path such as <c>url</c>, <c>items[0].url</c>, <c>items[*].url</c>, or <c>[*].url</c>.</param>
    /// <returns>A selected valid image URL, or null when not found.</returns>
    public static string? ExtractUrlByPath(string json, string path)
    {
        if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(path))
            return null;

        using JsonDocument document = JsonDocument.Parse(json);
        List<PathStep> steps = ParsePathSteps(path);
        List<string> rawMatches = [];
        CollectMatches(document.RootElement, steps, 0, rawMatches);

        List<string> staticUrls = [];
        List<string> fallbackUrls = [];
        foreach (string raw in rawMatches)
        {
            string? staticUrl = NormalizeImageUrl(raw, preferStaticOnly: true);
            if (staticUrl is not null)
                staticUrls.Add(staticUrl);
            else if (NormalizeImageUrl(raw, preferStaticOnly: false) is { } anyUrl)
                fallbackUrls.Add(anyUrl);
        }

        if (staticUrls.Count > 0)
            return staticUrls[CryptoRandom.NextIndex(staticUrls.Count)];
        if (fallbackUrls.Count > 0)
            return fallbackUrls[CryptoRandom.NextIndex(fallbackUrls.Count)];

        return null;
    }

    /// <summary>Inspects magic bytes to verify an image payload and determine its real file extension.</summary>
    /// <param name="bytes">Downloaded payload bytes.</param>
    /// <returns>Canonical extension such as <c>.jpg</c>, <c>.png</c>, <c>.webp</c>, <c>.bmp</c>, or null if invalid.</returns>
    public static string? DetectImageExtension(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < 32)
            return null;

        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return ".jpg";

        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
            return ".png";

        if (bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
            bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P')
            return ".webp";

        if (bytes[0] == (byte)'B' && bytes[1] == (byte)'M')
            return ".bmp";

        if (bytes[0] == (byte)'G' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'8')
            return ".gif";

        return null;
    }

    static async Task<DownloadedWallpaper> FetchDirectModeAsync(
        WallpaperProvider provider,
        WallpaperSubcategory subcategory,
        string endpoint,
        CancellationToken cancellationToken)
    {
        (byte[] body, string finalUrl) = await DownloadBytesAsync(endpoint, refererUrl: null, cancellationToken).ConfigureAwait(false);
        string? directExtension = DetectImageExtension(body);
        if (directExtension is not null)
            return new DownloadedWallpaper(body, directExtension, finalUrl, provider.Name, subcategory.Name);

        if (body.Length > 65536)
            throw new InvalidDataException($"Direct endpoint '{endpoint}' returned non-image binary payload ({body.Length} bytes).");

        string text = Encoding.UTF8.GetString(body).Trim();
        string? resolvedUrl = NormalizeImageUrl(text);
        if (resolvedUrl is null && text.StartsWith('{') && !string.IsNullOrWhiteSpace(provider.JsonPath))
            resolvedUrl = ExtractUrlByPath(text, provider.JsonPath);

        if (resolvedUrl is null)
            throw new InvalidDataException($"Direct endpoint '{endpoint}' did not return a valid image or URL.");

        (byte[] imageBytes, string resolvedFinalUrl) = await DownloadBytesAsync(resolvedUrl, endpoint, cancellationToken).ConfigureAwait(false);
        string? ext = DetectImageExtension(imageBytes);
        if (ext is null)
            throw new InvalidDataException($"Downloaded URL '{resolvedFinalUrl}' did not contain a valid image.");

        return new DownloadedWallpaper(imageBytes, ext, resolvedFinalUrl, provider.Name, subcategory.Name);
    }

    static async Task<string> ResolveJsonPathUrlAsync(
        WallpaperProvider provider,
        string endpoint,
        CancellationToken cancellationToken)
    {
        string json = await DownloadTextAsync(endpoint, cancellationToken).ConfigureAwait(false);
        string? url = ExtractUrlByPath(json, provider.JsonPath);
        if (url is null && !string.IsNullOrWhiteSpace(provider.FallbackJsonPath))
            url = ExtractUrlByPath(json, provider.FallbackJsonPath);

        return url ?? throw new InvalidDataException($"JSON path '{provider.JsonPath}' yielded no valid image URL from '{endpoint}'.");
    }

    static async Task<string> ResolveNekosMoeUrlAsync(string endpoint, CancellationToken cancellationToken)
    {
        string json = await DownloadTextAsync(endpoint, cancellationToken).ConfigureAwait(false);
        using JsonDocument doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("images", out JsonElement images) &&
            images.ValueKind == JsonValueKind.Array &&
            images.GetArrayLength() > 0)
        {
            List<string> ids = [];
            foreach (JsonElement item in images.EnumerateArray())
            {
                if (item.TryGetProperty("id", out JsonElement idProp) &&
                    idProp.ValueKind == JsonValueKind.String &&
                    !string.IsNullOrWhiteSpace(idProp.GetString()))
                {
                    ids.Add(idProp.GetString()!);
                }
            }

            if (ids.Count > 0)
                return $"https://nekos.moe/image/{ids[CryptoRandom.NextIndex(ids.Count)]}";
        }

        throw new InvalidDataException($"Nekos.moe response from '{endpoint}' contained no image IDs.");
    }

    static async Task<string> ResolveBooruPostUrlAsync(string endpoint, CancellationToken cancellationToken)
    {
        string json = await DownloadTextAsync(endpoint, cancellationToken).ConfigureAwait(false);
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement postsElement = doc.RootElement;
        if (postsElement.ValueKind == JsonValueKind.Object && postsElement.TryGetProperty("post", out JsonElement postArr))
            postsElement = postArr;
        else if (postsElement.ValueKind == JsonValueKind.Object && postsElement.TryGetProperty("posts", out JsonElement postsArr))
            postsElement = postsArr;

        if (postsElement.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException($"Booru response from '{endpoint}' was not a post array.");

        List<string> candidates = [];
        foreach (JsonElement post in postsElement.EnumerateArray())
        {
            if (post.ValueKind != JsonValueKind.Object)
                continue;

            foreach (string field in BooruUrlFields)
            {
                if (post.TryGetProperty(field, out JsonElement urlProp) &&
                    urlProp.ValueKind == JsonValueKind.String &&
                    NormalizeImageUrl(urlProp.GetString(), preferStaticOnly: true) is { } validUrl)
                {
                    candidates.Add(validUrl);
                    break;
                }
            }
        }

        if (candidates.Count == 0)
            throw new InvalidDataException($"Booru response from '{endpoint}' contained no static wallpaper URLs.");

        return candidates[CryptoRandom.NextIndex(candidates.Count)];
    }

    static async Task<string> ResolveGelbooruDapiUrlAsync(
        WallpaperProvider provider,
        WallpaperSubcategory subcategory,
        string endpoint,
        CancellationToken cancellationToken)
    {
        int page = subcategory.MaxRandomPage > 0 ? CryptoRandom.NextIndex(subcategory.MaxRandomPage + 1) : 0;
        string pagedUrl = AppendPageParameter(endpoint, page);
        List<string> candidates = await TryFetchGelbooruDapiCandidatesAsync(provider.ImageBaseUrl, pagedUrl, cancellationToken).ConfigureAwait(false);

        if (candidates.Count == 0 && page > 0)
        {
            string firstPageUrl = AppendPageParameter(endpoint, 0);
            candidates = await TryFetchGelbooruDapiCandidatesAsync(provider.ImageBaseUrl, firstPageUrl, cancellationToken).ConfigureAwait(false);
        }

        if (candidates.Count == 0)
            throw new InvalidDataException($"Gelbooru DAPI endpoint '{endpoint}' returned no static images.");

        return candidates[CryptoRandom.NextIndex(candidates.Count)];
    }

    static async Task<List<string>> TryFetchGelbooruDapiCandidatesAsync(
        string imageBaseUrl,
        string requestUrl,
        CancellationToken cancellationToken)
    {
        string json = await DownloadTextAsync(requestUrl, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json) || json.TrimStart() is not ['[', ..] and not ['{', ..])
            return [];

        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement array = doc.RootElement;
        if (array.ValueKind == JsonValueKind.Object && array.TryGetProperty("post", out JsonElement inner))
            array = inner;

        if (array.ValueKind != JsonValueKind.Array)
            return [];

        string basePrefix = imageBaseUrl.TrimEnd('/');
        List<string> results = [];

        foreach (JsonElement post in array.EnumerateArray())
        {
            if (post.ValueKind != JsonValueKind.Object)
                continue;

            string? candidate = null;
            if (post.TryGetProperty("file_url", out JsonElement fileUrlProp) && fileUrlProp.ValueKind == JsonValueKind.String)
                candidate = NormalizeImageUrl(fileUrlProp.GetString(), preferStaticOnly: true);

            if (candidate is null &&
                !string.IsNullOrWhiteSpace(basePrefix) &&
                post.TryGetProperty("directory", out JsonElement dirProp) &&
                post.TryGetProperty("image", out JsonElement imgProp))
            {
                string dirText = dirProp.ValueKind == JsonValueKind.Number ? dirProp.GetRawText() : dirProp.GetString() ?? string.Empty;
                string? imgText = imgProp.GetString();
                if (!string.IsNullOrWhiteSpace(dirText) && !string.IsNullOrWhiteSpace(imgText))
                    candidate = NormalizeImageUrl($"{basePrefix}/images/{dirText}/{imgText}", preferStaticOnly: true);
            }

            if (candidate is not null)
                results.Add(candidate);
        }

        return results;
    }

    static string AppendPageParameter(string url, int page)
    {
        char separator = url.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{url}{separator}pid={page}";
    }

    static string PickCandidateEndpoint(string endpointSpec)
    {
        if (!endpointSpec.Contains('|', StringComparison.Ordinal))
            return endpointSpec.Trim();

        string[] parts = endpointSpec.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            throw new InvalidOperationException("Subcategory endpoint is empty.");

        return parts[CryptoRandom.NextIndex(parts.Length)];
    }

    static Task<string> DownloadTextAsync(string url, CancellationToken cancellationToken) =>
        Resilience.ExecuteResilientlyAsync(
            async token =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain", 0.9));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.8));

                using HttpResponseMessage response = await Client.SendAsync(request, HttpCompletionOption.ResponseContentRead, token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                string content = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                Log($"HTTP text received; url={url}; status={(int)response.StatusCode}; chars={content.Length}", isRun: IsLogEnabled);
                return content;
            },
            ShouldRetryTransientHttp,
            cancellationToken,
            retryCount: 1,
            attemptTimeout: TimeSpan.FromSeconds(12),
            totalTimeout: TimeSpan.FromSeconds(22),
            operationName: "DownloadApiMetadata");

    static Task<(byte[] Bytes, string FinalUrl)> DownloadBytesAsync(string url, string? refererUrl, CancellationToken cancellationToken) =>
        Resilience.ExecuteResilientlyAsync(
            async token =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/*", 1.0));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.8));
                if (!string.IsNullOrWhiteSpace(refererUrl) && Uri.TryCreate(refererUrl, UriKind.Absolute, out Uri? refUri))
                    request.Headers.Referrer = new Uri($"{refUri.Scheme}://{refUri.Authority}/");

                using HttpResponseMessage response = await Client.SendAsync(request, HttpCompletionOption.ResponseContentRead, token).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                byte[] bytes = await response.Content.ReadAsByteArrayAsync(token).ConfigureAwait(false);
                string finalUrl = response.RequestMessage?.RequestUri?.AbsoluteUri ?? url;
                Log($"HTTP payload received; url={finalUrl}; status={(int)response.StatusCode}; bytes={bytes.Length}", isRun: IsLogEnabled);
                return (bytes, finalUrl);
            },
            ShouldRetryTransientHttp,
            cancellationToken,
            retryCount: 1,
            attemptTimeout: TimeSpan.FromSeconds(20),
            totalTimeout: TimeSpan.FromSeconds(35),
            operationName: "DownloadWallpaperPayload");

    static bool ShouldRetryTransientHttp(Exception exception)
    {
        if (exception is TimeoutRejectedException)
            return true;

        if (exception is HttpRequestException http)
        {
            if (http.InnerException is SocketException { SocketErrorCode: SocketError.HostNotFound or SocketError.NoData })
                return false;

            return http.StatusCode is null or HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests ||
                   (int)http.StatusCode.Value >= 500;
        }

        return false;
    }

    static HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            ConnectTimeout = TimeSpan.FromSeconds(8),
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 8
        };

        var client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        return client;
    }

    readonly record struct PathStep(string? PropertyName, int? ArrayIndex, bool WildcardArray);

    static List<PathStep> ParsePathSteps(string rawPath)
    {
        string cleaned = rawPath.Trim();
        if (cleaned.StartsWith("$.", StringComparison.Ordinal))
            cleaned = cleaned[2..];
        else if (cleaned.StartsWith('$'))
            cleaned = cleaned[1..];

        List<PathStep> steps = [];
        foreach (string segment in cleaned.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            int pos = 0;
            int firstBracket = segment.IndexOf('[');
            if (firstBracket < 0)
            {
                if (int.TryParse(segment, out int dotIndex) && dotIndex >= 0)
                    steps.Add(new PathStep(null, dotIndex, false));
                else if (segment == "*")
                    steps.Add(new PathStep(null, null, true));
                else
                    steps.Add(new PathStep(segment, null, false));
                continue;
            }

            if (firstBracket > 0)
            {
                steps.Add(new PathStep(segment[..firstBracket], null, false));
                pos = firstBracket;
            }

            while (pos < segment.Length)
            {
                int open = segment.IndexOf('[', pos);
                if (open < 0)
                    break;
                int close = segment.IndexOf(']', open + 1);
                if (close < 0)
                    break;

                string token = segment[(open + 1)..close].Trim().Trim('\'', '"');
                if (token == "*" || token.Length == 0)
                    steps.Add(new PathStep(null, null, true));
                else if (int.TryParse(token, out int idx) && idx >= 0)
                    steps.Add(new PathStep(null, idx, false));
                else
                    steps.Add(new PathStep(token, null, false));

                pos = close + 1;
            }
        }

        return steps;
    }

    static void CollectMatches(JsonElement current, List<PathStep> steps, int stepIndex, List<string> matches)
    {
        if (stepIndex >= steps.Count)
        {
            if (current.ValueKind == JsonValueKind.String && current.GetString() is { Length: > 0 } str)
                matches.Add(str);
            else if (current.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement child in current.EnumerateArray())
                {
                    if (child.ValueKind == JsonValueKind.String && child.GetString() is { Length: > 0 } itemStr)
                        matches.Add(itemStr);
                }
            }
            return;
        }

        PathStep step = steps[stepIndex];
        if (step.PropertyName is not null)
        {
            if (current.ValueKind == JsonValueKind.Object)
            {
                if (TryGetPropertyCaseInsensitive(current, step.PropertyName, out JsonElement next))
                    CollectMatches(next, steps, stepIndex + 1, matches);
            }
            else if (current.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in current.EnumerateArray())
                    CollectMatches(item, steps, stepIndex, matches);
            }
            return;
        }

        if (current.ValueKind != JsonValueKind.Array)
            return;

        if (step.WildcardArray)
        {
            foreach (JsonElement item in current.EnumerateArray())
                CollectMatches(item, steps, stepIndex + 1, matches);
        }
        else if (step.ArrayIndex is { } index && index >= 0 && index < current.GetArrayLength())
        {
            CollectMatches(current[index], steps, stepIndex + 1, matches);
        }
    }

    static bool TryGetPropertyCaseInsensitive(JsonElement obj, string name, out JsonElement value)
    {
        if (obj.TryGetProperty(name, out value))
            return true;

        foreach (JsonProperty prop in obj.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
