namespace NevaxisZentra.Desktop.Runtime;

public static class FactOsStartupErrorCodes
{
    public const string PreflightFailed = "ZENTRA-STARTUP-PREFLIGHT-001";
    public const string BundleMissing = "ZENTRA-STARTUP-BUNDLE-001";
    public const string DatabaseFailed = "ZENTRA-STARTUP-DB-001";
    public const string MigrationFailed = "ZENTRA-STARTUP-MIGRATION-001";
    public const string ApiFailed = "ZENTRA-STARTUP-API-001";
    public const string WebFailed = "ZENTRA-STARTUP-WEB-001";
    public const string PortCollision = "ZENTRA-STARTUP-PORT-001";
    public const string PrerequisiteMissing = "ZENTRA-STARTUP-PREREQ-001";
    public const string Unknown = "ZENTRA-STARTUP-UNKNOWN-001";

    public static string MapException(Exception exception) =>
        exception switch
        {
            StartupPreflightException preflight => preflight.ErrorCode,
            InvalidOperationException invalid when invalid.Message.Contains("PostgreSQL", StringComparison.OrdinalIgnoreCase)
                => DatabaseFailed,
            InvalidOperationException invalid when invalid.Message.Contains("API", StringComparison.OrdinalIgnoreCase)
                => ApiFailed,
            InvalidOperationException invalid when invalid.Message.Contains("Web", StringComparison.OrdinalIgnoreCase)
                => WebFailed,
            _ => Unknown,
        };

    public static string ToCustomerMessage(string errorCode) => errorCode switch
    {
        PreflightFailed => "Başlatma ön kontrolleri başarısız oldu.",
        BundleMissing => "Kurulum dosyaları eksik veya bozuk görünüyor.",
        DatabaseFailed => "Veritabanı çalışma bileşeni başlatılamadı.",
        MigrationFailed => "Veritabanı şeması güncellenemedi.",
        ApiFailed => "Nevaxis Zentra API servisi başlatılamadı.",
        WebFailed => "Nevaxis Zentra web uygulaması başlatılamadı.",
        PortCollision => "Gerekli yerel bağlantı noktaları kullanımda.",
        PrerequisiteMissing => "Gerekli bir sistem bileşeni eksik.",
        _ => "Nevaxis Zentra başlatılamadı.",
    };
}
