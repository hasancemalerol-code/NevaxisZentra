namespace CrevaFactOS.Persistence.Teams;

public static class TeamsPresentationBuilder
{
    public static string BuildTeamsChannelDeepLink(Guid channelId) =>
        $"/teams?channel={channelId:D}";

    public static string BuildTeamsMessageDeepLink(Guid channelId, Guid messageId) =>
        TeamsMessageTaskPresentationBuilder.BuildMessageDeepLink(channelId, messageId);

    public static string BuildAiTaskDeepLink(Guid taskId) =>
        $"/ai-gorevleri/{taskId:D}";

    public static TeamsSourceContextReadModel? BuildAiTaskSourceContext(
        Guid taskId,
        string? taskTitle,
        bool canOpen) =>
        new(
            "ai-task",
            string.IsNullOrWhiteSpace(taskTitle) ? "AI Görevi" : taskTitle,
            canOpen ? BuildAiTaskDeepLink(taskId) : null,
            taskId,
            null,
            null,
            canOpen);

    public static TeamsSourceContextReadModel? BuildOperationalSourceContext(
        string sourceModule,
        string sourceEntityId,
        bool canOpen)
    {
        var mapping = ResolveOperationalDeepLink(sourceModule, sourceEntityId);
        if (mapping is null)
        {
            return new TeamsSourceContextReadModel(
                "operational",
                FormatSourceModuleLabel(sourceModule),
                null,
                null,
                sourceModule,
                sourceEntityId,
                false);
        }

        return new TeamsSourceContextReadModel(
            "operational",
            mapping.Value.Label,
            canOpen ? mapping.Value.Href : null,
            null,
            sourceModule,
            sourceEntityId,
            canOpen);
    }

    public static string FormatSourceModuleLabel(string sourceModule) =>
        sourceModule switch
        {
            "commercial-intelligence" => "Ticari zekâ",
            "operational-risk" => "Operasyonel risk",
            "export" => "Dışa aktarım",
            "maintenance" => "Bakım",
            "quality" => "Kalite",
            "material" => "Malzeme",
            "mold" => "Kalıp",
            "production" => "Üretim",
            "procurement" => "Satın alma",
            "production-planning" => "Üretim planlama",
            "teams" => "Teams mesajı",
            _ => sourceModule,
        };

    private static (string Href, string Label)? ResolveOperationalDeepLink(
        string sourceModule,
        string sourceEntityId)
    {
        var encoded = Uri.EscapeDataString(sourceEntityId);
        return sourceModule switch
        {
            "maintenance" => ($"/bakim/kestirimci-riskler?assessment={encoded}", "Kestirimci bakım riski"),
            "quality" => ($"/kalite/takip-ve-gorevler?finding={encoded}", "Kalite uygunsuzluk kaydı"),
            "material" => ($"/stok-ve-depo/malzeme-riskleri?assessment={encoded}", "Malzeme risk değerlendirmesi"),
            "mold" => ($"/kaliplar/takip-ve-gorevler?mold={encoded}", "Kalıp takip kaydı"),
            "production" => ($"/uretim/istisnalar-ve-gorevler?exception={encoded}", "Üretim istisnası"),
            "procurement" => ($"/satin-alma/tedarik-riskleri?assessment={encoded}", "Tedarik risk değerlendirmesi"),
            "production-planning" => ($"/planlama?plan={encoded}", "Üretim planı"),
            "operational-risk" => ($"/operasyonel-riskler?case={encoded}", "Operasyonel risk"),
            _ => null,
        };
    }
}
