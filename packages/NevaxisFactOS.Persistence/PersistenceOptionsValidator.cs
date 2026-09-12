using Microsoft.Extensions.Options;
using Npgsql;

namespace NevaxisFactOS.Persistence;

public sealed class PersistenceOptionsValidator : IValidateOptions<PersistenceOptions>
{
    public ValidateOptionsResult Validate(string? name, PersistenceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();

        AddRequiredFailure(failures, options.Host, "Persistence:Host");
        AddRequiredFailure(failures, options.Database, "Persistence:Database");
        AddRequiredFailure(failures, options.Username, "Persistence:Username");
        AddRequiredFailure(failures, options.Password, "Persistence:Password");

        if (options.Port is < 1 or > 65535)
        {
            failures.Add("Persistence:Port must be between 1 and 65535 when persistence is enabled.");
        }

        if (options.ConnectionTimeoutSeconds <= 0)
        {
            failures.Add("Persistence:ConnectionTimeoutSeconds must be greater than zero.");
        }

        if (options.CommandTimeoutSeconds <= 0)
        {
            failures.Add("Persistence:CommandTimeoutSeconds must be greater than zero.");
        }

        if (!Enum.TryParse<SslMode>(options.SslMode, ignoreCase: true, out _))
        {
            failures.Add("Persistence:SslMode must be a valid Npgsql SSL mode.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void AddRequiredFailure(List<string> failures, string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{key} is required when persistence is enabled.");
        }
    }
}
