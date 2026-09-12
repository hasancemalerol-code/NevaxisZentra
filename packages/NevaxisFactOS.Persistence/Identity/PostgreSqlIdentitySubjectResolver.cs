using Microsoft.Extensions.Options;
using Npgsql;

namespace NevaxisFactOS.Persistence.Identity;

internal sealed class PostgreSqlIdentitySubjectResolver(IOptions<PersistenceOptions> options)
    : IIdentitySubjectResolver
{
    public async Task<AuthenticatedIdentity?> ResolveActiveIdentityAsync(
        string externalIssuer,
        string externalSubject,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalIssuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalSubject);

        await using var connection = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(options.Value));
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT identity_subject_id FROM resolve_active_identity(@issuer, @subject)",
            connection);
        command.Parameters.AddWithValue("issuer", externalIssuer);
        command.Parameters.AddWithValue("subject", externalSubject);
        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is Guid identitySubjectId
            ? new AuthenticatedIdentity(identitySubjectId, externalIssuer, externalSubject)
            : null;
    }
}
