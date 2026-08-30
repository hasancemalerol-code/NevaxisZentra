using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CrevaFactOS.Persistence.Teams;

public static class TeamsIdempotencyFingerprint
{
    public static string ComputeMessageFingerprint(
        TeamsOperationContext context,
        PostTeamsMessageCommand command)
    {
        var payload = new
        {
            tenantId = context.TenantId,
            facilityId = context.FacilityId,
            channelId = command.ChannelId,
            authorMembershipId = context.MembershipId,
            body = command.Body.Trim(),
            aiTaskId = command.AiTaskId,
            sourceModule = NormalizeOptional(command.SourceModule),
            sourceEntityId = NormalizeOptional(command.SourceEntityId),
            mentions = command.MentionedMembershipIds.OrderBy(id => id).ToArray(),
        };

        var json = JsonSerializer.Serialize(payload);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ComputeDirectConversationFingerprint(
        TeamsOperationContext context,
        CreateTeamsDirectConversationCommand command)
    {
        var payload = new
        {
            tenantId = context.TenantId,
            facilityId = context.FacilityId,
            initiatorMembershipId = context.MembershipId,
            recipientMembershipId = command.RecipientTenantMembershipId,
        };

        var json = JsonSerializer.Serialize(payload);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ComputeDirectMessageFingerprint(
        TeamsOperationContext context,
        PostTeamsDirectMessageCommand command)
    {
        var payload = new
        {
            tenantId = context.TenantId,
            facilityId = context.FacilityId,
            conversationId = command.ConversationId,
            authorMembershipId = context.MembershipId,
            body = command.Body.Trim(),
        };

        var json = JsonSerializer.Serialize(payload);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
