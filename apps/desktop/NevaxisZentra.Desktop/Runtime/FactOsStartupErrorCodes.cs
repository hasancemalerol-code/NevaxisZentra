namespace NevaxisZentra.Desktop.Runtime;

public static class FactOsStartupErrorCodes
{
    public const string PreflightFailed = "FACTOS-STARTUP-PREFLIGHT-001";
    public const string BundleMissing = "FACTOS-STARTUP-BUNDLE-001";
    public const string DatabaseFailed = "FACTOS-STARTUP-DB-001";
    public const string MigrationFailed = "FACTOS-STARTUP-MIGRATION-001";
    public const string ApiFailed = "FACTOS-STARTUP-API-001";
    public const string WebFailed = "FACTOS-STARTUP-WEB-001";
    public const string PortCollision = "FACTOS-STARTUP-PORT-001";
    public const string PrerequisiteMissing = "FACTOS-STARTUP-PREREQ-001";
    public const string Unknown = "FACTOS-STARTUP-UNKNOWN-001";

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
        ApiFailed => "FactOS API servisi başlatılamadı.",
        WebFailed => "FactOS web uygulaması başlatılamadı.",
        PortCollision => "Gerekli yerel bağlantı noktaları kullanımda.",
        PrerequisiteMissing => "Gerekli bir sistem bileşeni eksik.",
        _ => "FactOS başlatılamadı.",
    };
}
