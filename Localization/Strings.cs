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
    public static string AppDescription = "Her çalıştırmada yeni anime duvar kağıdı indirir ve uygular.";

    // Language.
    public static string SettingLanguage = "Dil";
    public static string LanguageSystem = "Windows'u izle";

    // Settings.
    public static string SettingsTitle = "Ayarlar";
    public static string SettingImageSaveDirectory = "Resim kayıt klasörü";
    public static string SettingEnableLogging = "Günlük kaydı açık";
    public static string SettingsSaved = "Ayarlar kaydedildi.";

    // Commands.
    public static string ButtonSave = "Kaydet";
    public static string ButtonCancel = "Vazgeç";
    public static string ButtonRetry = "Yeniden dene";
    public static string ButtonClose = "Kapat";
    public static string ButtonOpenFolder = "Klasörü aç";

    // Operation status.
    public static string StatusReady = "Hazır";
    public static string StatusWorking = "İşlem sürüyor.";
    public static string StatusCompleted = "İşlem tamamlandı.";
    public static string StatusCanceled = "İşlem iptal edildi.";
    public static string StatusFailed = "İşlem tamamlanamadı.";
    public static string ProgressItems = "{0} / {1} öğe işlendi.";

    // User-editable text supports escaped newlines in the XML files.
    public static string StatusDownloadingMetadata = "Görsel bilgisi alınıyor: {0}";
    public static string StatusDownloadingImage = "Görsel indiriliyor: {0}";
    public static string StatusImageSaved = "Görsel kaydedildi: {0}";
    public static string StatusImageUrlMissing = "Sunucu yanıtında görsel adresi bulunamadı.";
    public static string StatusExtensionFallback = "Dosya uzantısı belirlenemedi; .jpg kullanılacak.";
    public static string StatusFileNameFallback = "Dosya adı belirlenemedi; yeni bir ad kullanılacak.";
    public static string StatusApplicationNameMissing = "Uygulama dosyasının adı okunamadı.";
}
