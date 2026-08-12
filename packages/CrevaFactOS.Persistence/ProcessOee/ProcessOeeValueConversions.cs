namespace CrevaFactOS.Persistence.ProcessOee;

public static class ProcessOeeValueConversions
{
    public static string NormalizeSourceType(string? sourceType)
    {
        var normalized = (sourceType ?? ProcessOeeSourceTypes.Manual).Trim().ToLowerInvariant();
        return normalized switch
        {
            ProcessOeeSourceTypes.Manual => ProcessOeeSourceTypes.Manual,
            ProcessOeeSourceTypes.Imported => ProcessOeeSourceTypes.Imported,
            ProcessOeeSourceTypes.Erp => ProcessOeeSourceTypes.Erp,
            ProcessOeeSourceTypes.DevelopmentPilot => ProcessOeeSourceTypes.DevelopmentPilot,
            ProcessOeeSourceTypes.Unknown => ProcessOeeSourceTypes.Unknown,
            _ => ProcessOeeSourceTypes.Unknown,
        };
    }

    public static string ToDisplaySourceType(string sourceType) => sourceType switch
    {
        ProcessOeeSourceTypes.Manual => "Manuel",
        ProcessOeeSourceTypes.Imported => "İçe aktarım",
        ProcessOeeSourceTypes.Erp => "ERP",
        ProcessOeeSourceTypes.DevelopmentPilot => "Geliştirme pilotu",
        ProcessOeeSourceTypes.Unknown => "Bilinmiyor",
        _ => sourceType,
    };

    public static string ToDisplayStatus(string status) => status switch
    {
        ProcessOeeAssessmentStatuses.Calculated => "Hesaplandı",
        ProcessOeeAssessmentStatuses.InsufficientData => "Veri yetersiz",
        ProcessOeeAssessmentStatuses.ReviewRequired => "İnceleme gerekli",
        ProcessOeeAssessmentStatuses.StaleData => "Bayat veri",
        _ => status,
    };
}
