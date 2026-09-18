using Microsoft.Extensions.Options;
using NevaxisZentra.Persistence.SaasIdentity;
using Npgsql;

namespace NevaxisZentra.Persistence.Identity;

internal sealed class PostgreSqlIdentitySubjectResolver(IOptions<PersistenceOptions> options)
    : IIdentitySubjectResolver
{
    private static readonly string[] LegacySaasIdentityIssuers =
    [
        "crevafactos-saas",
        "NevaxisFactOS-saas",
    ];

    public async Task<AuthenticatedIdentity?> ResolveActiveIdentityAsync(
        string externalIssuer,
        string externalSubject,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalIssuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalSubject);

        foreach (var issuerCandidate in EnumerateIssuerCandidates(externalIssuer))
        {
            var identitySubjectId = await TryResolveIdentitySubjectIdAsync(
                issuerCandidate,
                externalSubject,
                cancellationToken);
            if (identitySubjectId is not null)
            {
                return new AuthenticatedIdentity(
                    identitySubjectId.Value,
                    issuerCandidate,
                    externalSubject);
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateIssuerCandidates(string externalIssuer)
    {
        yield return externalIssuer;

        if (!string.Equals(externalIssuer, SaasIdentityConstants.Issuer, StringComparison.Ordinal))
        {
            yield break;
        }

        foreach (var legacyIssuer in LegacySaasIdentityIssuers)
        {
            if (!string.Equals(legacyIssuer, externalIssuer, StringComparison.Ordinal))
            {
                yield return legacyIssuer;
            }
        }
    }

    private async Task<Guid?> TryResolveIdentitySubjectIdAsync(
        string externalIssuer,
        string externalSubject,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(options.Value));
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT identity_subject_id FROM resolve_active_identity(@issuer, @subject)",
            connection);
        command.Parameters.AddWithValue("issuer", externalIssuer);
        command.Parameters.AddWithValue("subject", externalSubject);
        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is Guid identitySubjectId ? identitySubjectId : null;
    }
}
