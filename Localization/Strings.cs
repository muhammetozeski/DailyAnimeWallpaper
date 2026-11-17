namespace DailyAnimeWallpaper.Localization;

/// <summary>
/// User-facing UI strings, Turkish by default. <see cref="LocManager"/> reflection-writes these
/// fields to <c>lang.tr.xml</c> when it is missing, and loads matching XML entries when a language
/// is selected. Keep each public string field mutable so the existing loader can update it.
/// Add the same key and matching format placeholders to lang.en.xml for every new field.
/// </summary>
internal static class Strings
{
    private const bool IsLogEnabled = true;

    // Application.
    public static string AppTitle = "Daily Anime Wallpaper";
    public static string AppDescription = "Her çalıştırmada seçili anime API kaynaklarından duvar kağıdı indirir ve uygular.";

    // Language.
    public static string SettingLanguage = "Dil";
    public static string LanguageSystem = "Windows'u izle";

    // Settings.
    public static string SettingsTitle = "Daily Anime Wallpaper - Ayarlar";
    public static string GroupGeneralSettings = "Genel Ayarlar ve Filtreler";
    public static string SettingImageSaveDirectory = "Resim kayıt klasörü";
    public static string SettingEnableLogging = "Günlük kaydı açık";
    public static string SettingNsfwFilter = "NSFW filtresi";
    public static string FilterUseSelection = "Tür seçimlerine uy";
    public static string FilterSfwOnly = "Yalnızca SFW (NSFW engelle)";
    public static string FilterNsfwOnly = "Yalnızca NSFW";
    public static string SettingsSaved = "Ayarlar ve API listesi kaydedildi.";

    // Providers & Subcategories UI.
    public static string GroupProviders = "API Kaynakları";
    public static string GroupSubcategories = "İç Türler ({0})";
    public static string GroupSubcategoriesNone = "İç Türler (API seçin)";
    public static string LabelSearchApi = "Ara:";
    public static string LabelWeight = "Ağırlık (0 - 2147483647):";
    public static string LabelNsfwTag = "NSFW tür";
    public static string SummaryActiveCount = "{0} / {1} etkin";

    public static string ColumnApiName = "API Kaynağı";
    public static string ColumnMode = "Tür";
    public static string ColumnWeight = "Ağırlık";
    public static string ColumnActiveSubcategories = "Etkin Tür";
    public static string ColumnSubcategoryName = "İç Tür";
    public static string ColumnRating = "İçerik";
    public static string ColumnEndpoint = "Adres / Uç Nokta";

    public static string ModeJsonPath = "JSON Yolu";
    public static string ModeDirectUrlOrImage = "Direkt Resim / URL";
    public static string ModeBuiltInSpecial = "Yerleşik Özel";
    public static string RatingSfw = "SFW";
    public static string RatingNsfw = "NSFW";

    // Commands.
    public static string ButtonSave = "Kaydet";
    public static string ButtonCancel = "Vazgeç";
    public static string ButtonRetry = "Yeniden dene";
    public static string ButtonClose = "Kapat";
    public static string ButtonBrowse = "Gözat...";
    public static string ButtonOpenFolder = "Klasörü aç";
    public static string ButtonAddApi = "API Ekle";
    public static string ButtonEdit = "Düzenle";
    public static string ButtonRemove = "Kaldır";
    public static string ButtonSelectAll = "Tümünü Seç";
    public static string ButtonSelectNone = "Hiçbiri";
    public static string ButtonEqualizeWeights = "Eşit Ağırlık";
    public static string ButtonAddSubcategory = "Tür Ekle";
    public static string ButtonSelectSfw = "SFW Seç";
    public static string ButtonUncheckNsfw = "NSFW Kaldır";
    public static string ButtonRestoreDefaults = "Varsayılanları Yükle";
    public static string ButtonTestSelected = "Seçiliyi Test Et";
    public static string ButtonApplyNow = "Şimdi Duvar Kağıdı Değiştir";

    // Dialogs.
    public static string DialogAddProviderTitle = "Yeni Anime API Kaynağı Ekle";
    public static string DialogEditProviderTitle = "API Kaynağını Düzenle";
    public static string DialogAddSubcategoryTitle = "Yeni İç Tür Ekle";
    public static string DialogEditSubcategoryTitle = "İç Türü Düzenle";
    public static string FieldProviderName = "API adı:";
    public static string FieldResponseMode = "Yanıt biçimi:";
    public static string FieldJsonPath = "JSON resim adresi yolu:";
    public static string FieldFallbackJsonPath = "Yedek JSON yolu (isteğe bağlı):";
    public static string FieldDefaultEndpoint = "Genel (varsayılan) API adresi:";
    public static string FieldSubcategoryName = "İç tür adı:";
    public static string FieldEndpointUrl = "API / Tür adresi (URL):";
    public static string FieldEnabled = "Etkin";
    public static string HintJsonPath = "Örnek: url, items[0].url, items[*].url, data.original.url";
    public static string ValidationNameRequired = "Lütfen bir ad girin.";
    public static string ValidationEndpointRequired = "Lütfen geçerli bir HTTP/HTTPS adresi girin.";
    public static string ValidationJsonPathRequired = "JSON yanıt biçimi için JSON resim yolu gereklidir.";
    public static string ConfirmRestoreDefaults = "Tüm yerleşik API kaynakları ve iç türler varsayılan listeye sıfırlansın mı?";

    // Operation status.
    public static string StatusReady = "Hazır";
    public static string StatusWorking = "İşlem sürüyor.";
    public static string StatusCompleted = "İşlem tamamlandı.";
    public static string StatusCanceled = "İşlem iptal edildi.";
    public static string StatusFailed = "İşlem tamamlanamadı.";
    public static string ProgressItems = "{0} / {1} öğe işlendi.";
    public static string StatusTryingSource = "Deneniyor: {0} / {1}...";
    public static string StatusAppliedFromSource = "Duvar kağıdı uygulandı: {0} / {1}";
    public static string StatusDownloadingMetadata = "Görsel bilgisi alınıyor: {0}";
    public static string StatusDownloadingImage = "Görsel indiriliyor: {0}";
    public static string StatusImageSaved = "Görsel kaydedildi: {0}";
    public static string StatusImageUrlMissing = "Sunucu yanıtında görsel adresi bulunamadı.";
    public static string StatusExtensionFallback = "Dosya uzantısı belirlenemedi; .jpg kullanılacak.";
    public static string StatusFileNameFallback = "Dosya adı belirlenemedi; yeni bir ad kullanılacak.";
    public static string StatusApplicationNameMissing = "Uygulama dosyasının adı okunamadı.";
    public static string ErrorNoEligibleSources = "Etkin ve ağırlığı 0'dan büyük hiçbir uygun API veya iç tür seçili değil.";
    public static string ErrorAllSourcesFailed = "Etkin API kaynaklarının tamamı denendi ancak görsel indirilemedi.";
}
