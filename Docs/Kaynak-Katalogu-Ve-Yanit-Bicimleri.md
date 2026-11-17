# Kaynak Kataloğu ve Yanıt Biçimleri

Bu belge, `BuiltInCatalog`, `WallpaperClient` ve `UserData/sources.json` üzerinde yeni anime/booru kaynağı eklerken veya mevcut kaynakları güncellerken dikkate alınması gereken doğrulanmış kuralları içerir.

## 1. Yanıt modları (`ProviderResponseMode`)

`WallpaperClient.FetchWallpaperAsync` beş yanıt modunu destekler; kullanıcı arayüzünden eklenen özel kaynaklar ilk iki modu kullanır, yerleşik kaynaklar beş modun tamamını kullanabilir:

- **`JsonElementPath`:** Yanıt gövdesinden nokta ve köşeli parantez yoluyla (`url`, `items[0].url`, `items[*].url`, `[*].url`, `image.original.url`) görsel adresini çıkarır. Yol üzerinde JSON dizisi bulunduğunda ve sabit indeks verilmediğinde geçerli durağan görsel adresleri toplanır ve `CryptoRandom.NextIndex` ile seçilir. Birincil yol sonuç üretmezse `FallbackJsonPath` denenir.
- **`DirectUrlOrImage`:** İstek yanıtının ilk baytlarını `DetectImageExtension` ile kontrol eder. Yanıt doğrudan JPEG, PNG, WebP, BMP veya GIF baytları taşıyorsa (301/302 yönlendirmeleri dahil) ikinci bir istek atmadan görseli kullanır; metin döndürüyorsa düz metin URL'yi normalize edip görseli indirir.
- **`NekosMoeId`:** `https://nekos.moe/api/v1/random/image` yanıtındaki `images[*].id` değerini okur ve `https://nekos.moe/image/{id}` adresinden görseli indirir.
- **`BooruPostList`:** Moebooru (`Konachan.net`, `Yande.re`), `Danbooru` ve `AIBooru` gönderi dizilerinde sırasıyla `file_url`, `large_file_url`, `jpeg_url` ve `sample_url` alanlarını kontrol eder; video, arşiv ve animasyonlu GIF uzantılarını eleyip durağan görseller arasından seçim yapar.
- **`GelbooruDapi`:** `Safebooru.org` ve `TBIB` JSON DAPI uç noktaları `order:random` desteklemediğinden `MaxRandomPage` sınırında rastgele `pid` sayfası ister; seçilen sayfa boş dönerse `pid=0` ile yedek sorgu yapar. `file_url` yoksa `{ImageBaseUrl}/images/{directory}/{image}` adresini kurar (`TBIB` üzerinde `hash` değil `image` alanı geçerlidir).

## 2. Canlı testlerde doğrulanmış API davranışları (Kasım 2026)

- **Şemasız URL dönüşü (`Pic.re`):** `https://pic.re/image.json` yanıtındaki `file_url` alanı `"cdn.pic.re/..."` biçiminde `https://` öneki olmadan dönebilir. `WallpaperClient.NormalizeImageUrl` ilk `/` öncesinde alan adı noktası gördüğünde `https://` önekini otomatik tamamlar.
- **Zorunlu `User-Agent` başlığı:** `Nekos.best`, `Danbooru` ve Moebooru sunucuları boş `User-Agent` ile gelen `.NET HttpClient` isteklerini reddedebilir; ortak istemci `AppConstants.UserAgent` başlığını her istekte gönderir.
- **Uzantısız uç noktalar ve WebP duvar kağıdı:** `Imlazy`, `Horosama`, `Yppp` ve `Paulzzh Touhou` gibi uç noktaların URL yolunda gerçek dosya uzantısı bulunmaz veya `.webp` döner. Kayıt uzantısı URL'den değil `DetectImageExtension` sihirli bayt kontrolünden belirlenir; HTML hata sayfalarının görsel diye kaydedilmesi bu kontrolle engellenir. Windows 11 üzerinde `SystemParametersInfo(SPI_SETDESKWALLPAPER)` çağrısının `.webp`, `.png` ve `.jpg` dosyalarını doğrudan uyguladığı doğrulanmıştır.
- **DNS hatasında hızlı geçiş:** Alan adı kaydı silinmiş servislerde (`SocketError.HostNotFound` / `NoData`) Polly tekrarı yapılmaz; `ShouldRetryTransientHttp` yalnız zaman aşımı, `408`, `429` ve `5xx` hatalarını bir kez tekrarlar ve başarısız kaynak havuzdan çıkarılarak sıradaki ağırlıklı kaynağa geçilir.
- **Kapanmış veya istemciye kapalı servisler:** Kasım 2026 itibarıyla `api.waifu.im` (24 Ekim 2026'da kalıcı olarak kapandı), `api.waifu.pics` (DNS kaydı yok), `konachan.com` / `safebooru.donmai.us` / `anime-pictures.net` (Cloudflare 403), `gelbooru.com` (kimlik doğrulama zorunlu 401) ve `zerochan.net` (bozuk JSON / HTTP 500) yerleşik listeye eklenmemelidir.

## 3. Katalog kalıcılığı ve ağırlıklı seçim sözleşmesi

- Kalıcı katalog `UserData/sources.json` içinde tutulur. Kullanıcı yerleşik bir kaynağı arayüzden kaldırdığında kimliği `RemovedBuiltInIds` listesine yazılır; böylece sonraki açılışta silinen yerleşik kaynak kendiliğinden geri eklenmez, yeni sürümle gelen farklı yerleşik kaynaklar ise mevcut kullanıcı düzenine eklenir.
- Her API ve iç tür `[0, int.MaxValue]` aralığında ağırlık alır. `CryptoRandom.SelectWeighted` pozitif ağırlıkları `UInt128` içinde toplar ve `RandomNumberGenerator.Fill` ile doldurulan 16 baytlık `UInt128` değerinden seçim yapar (`System.Random` veya `BigInteger` kullanılmaz).
