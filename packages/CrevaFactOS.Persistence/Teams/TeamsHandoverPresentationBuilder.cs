using System.Globalization;

namespace CrevaFactOS.Persistence.Teams;

public static class TeamsHandoverPresentationBuilder
{
    public static string? ValidateCreateInput(CreateTeamsHandoverCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey) || command.IdempotencyKey.Length > 128)
        {
            return "Geçerli bir idempotency anahtarı gerekli.";
        }

        return ValidateDraftFields(command.ShiftDate, command.ShiftType, command.Summary, command.SafetyNote, command.Items);
    }

    public static string? ValidateDraftFields(
        DateOnly shiftDate,
        string shiftType,
        string summary,
        string safetyNote,
        IReadOnlyList<TeamsHandoverItemInput> items)
    {
        if (shiftDate == default)
        {
            return "Vardiya tarihi zorunludur.";
        }

        if (!IsShiftType(shiftType))
        {
            return "Geçerli bir vardiya türü seçin.";
        }

        if (string.IsNullOrWhiteSpace(summary) || summary.Trim().Length > TeamsConstants.MaxHandoverSummaryLength)
        {
            return "Özet zorunludur ve 2000 karakteri aşamaz.";
        }

        if (safetyNote is not null && safetyNote.Trim().Length > TeamsConstants.MaxHandoverSafetyNoteLength)
        {
            return "Güvenlik notu 2000 karakteri aşamaz.";
        }

        if (items is null || items.Count == 0)
        {
            return "En az bir devir maddesi ekleyin.";
        }

        if (items.Count > TeamsConstants.MaxHandoverItems)
        {
            return "En fazla 20 devir maddesi eklenebilir.";
        }

        foreach (var item in items)
        {
            if (!IsItemKind(item.ItemKind))
            {
                return "Geçersiz madde türü.";
            }

            if (string.IsNullOrWhiteSpace(item.Title) || item.Title.Trim().Length > TeamsConstants.MaxHandoverItemTitleLength)
            {
                return "Madde başlığı zorunludur ve 160 karakteri aşamaz.";
            }

            if (string.IsNullOrWhiteSpace(item.Body) || item.Body.Trim().Length > TeamsConstants.MaxHandoverItemBodyLength)
            {
                return "Madde açıklaması zorunludur ve 2000 karakteri aşamaz.";
            }

            var hasMessage = item.MessageId is Guid messageId && messageId != Guid.Empty;
            var hasTask = item.AiTaskId is Guid taskId && taskId != Guid.Empty;
            if (hasMessage && hasTask)
            {
                return "Bir madde yalnızca bir Teams mesajına veya bir AI görevine bağlanabilir.";
            }
        }

        return null;
    }

    public static bool IsShiftType(string? value) =>
        TeamsHandoverShiftTypes.Morning.Equals(value, StringComparison.Ordinal)
        || TeamsHandoverShiftTypes.Evening.Equals(value, StringComparison.Ordinal)
        || TeamsHandoverShiftTypes.Night.Equals(value, StringComparison.Ordinal)
        || TeamsHandoverShiftTypes.Custom.Equals(value, StringComparison.Ordinal);

    public static bool IsItemKind(string? value) =>
        TeamsHandoverItemKinds.Attention.Equals(value, StringComparison.Ordinal)
        || TeamsHandoverItemKinds.Incident.Equals(value, StringComparison.Ordinal)
        || TeamsHandoverItemKinds.PendingWork.Equals(value, StringComparison.Ordinal)
        || TeamsHandoverItemKinds.QualityNote.Equals(value, StringComparison.Ordinal)
        || TeamsHandoverItemKinds.MaintenanceNote.Equals(value, StringComparison.Ordinal)
        || TeamsHandoverItemKinds.MaterialNote.Equals(value, StringComparison.Ordinal);

    public static bool IsHandoverChannelType(string channelType) =>
        TeamsChannelTypes.Department.Equals(channelType, StringComparison.Ordinal)
        || TeamsChannelTypes.Operational.Equals(channelType, StringComparison.Ordinal);

    public static string BuildDeepLink(Guid handoverId) => $"/teams/devretme/{handoverId:D}";

    public static string BuildMessageDeepLink(Guid channelId, Guid messageId) =>
        $"/teams?channel={channelId:D}&message={messageId:D}";

    public static string BuildTaskDeepLink(Guid taskId) => $"/ai-gorevleri/{taskId:D}";

    public static string BuildCreateFingerprint(
        Guid channelId,
        DateOnly shiftDate,
        string shiftType,
        string summary,
        string safetyNote,
        IReadOnlyList<TeamsHandoverItemInput> items) =>
        string.Join("|",
            channelId.ToString("D"),
            shiftDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            shiftType,
            summary.Trim(),
            safetyNote.Trim(),
            string.Join(";", items.Select(i =>
                $"{i.ItemKind}:{i.Title.Trim()}:{i.Body.Trim()}:{i.MessageId:D}:{i.AiTaskId:D}")));
}
