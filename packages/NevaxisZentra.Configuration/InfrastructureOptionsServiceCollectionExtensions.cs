using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace NevaxisZentra.Configuration;

public static class InfrastructureOptionsServiceCollectionExtensions
{
    public static OptionsBuilder<InfrastructureOptions> AddInfrastructureOptions(
        this IServiceCollection services,
        IConfiguration configuration,
        InfrastructureDependency requiredDependencies = InfrastructureDependency.None)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var builder = services
            .AddOptions<InfrastructureOptions>()
            .Bind(configuration.GetSection(InfrastructureOptions.SectionName))
            .PostConfigure(options => ApplyPrefixedEnvironment(configuration, options));

        if (requiredDependencies.HasFlag(InfrastructureDependency.PostgreSql))
        {
            builder.Validate(
                static options => IsValid(options.PostgreSql),
                "PostgreSQL infrastructure configuration is incomplete or invalid.");
        }

        if (requiredDependencies.HasFlag(InfrastructureDependency.RabbitMq))
        {
            builder.Validate(
                static options => IsValid(options.RabbitMq),
                "RabbitMQ infrastructure configuration is incomplete or invalid.");
        }

        if (requiredDependencies != InfrastructureDependency.None)
        {
            builder.ValidateOnStart();
        }

        return builder;
    }

    private static void ApplyPrefixedEnvironment(
        IConfiguration configuration,
        InfrastructureOptions options)
    {
        ApplyResolvedString(configuration, "POSTGRES_HOST", value => options.PostgreSql.Host = value);
        ApplyResolvedInt(configuration, "POSTGRES_PORT", value => options.PostgreSql.Port = value);
        ApplyResolvedString(configuration, "POSTGRES_DATABASE", value => options.PostgreSql.Database = value);
        ApplyResolvedString(configuration, "POSTGRES_USERNAME", value => options.PostgreSql.Username = value);
        ApplyResolvedString(configuration, "POSTGRES_PASSWORD", value => options.PostgreSql.Password = value);

        ApplyResolvedString(configuration, "RABBITMQ_HOST", value => options.RabbitMq.Host = value);
        ApplyResolvedInt(configuration, "RABBITMQ_AMQP_PORT", value => options.RabbitMq.AmqpPort = value);
        ApplyResolvedInt(configuration, "RABBITMQ_MANAGEMENT_PORT", value => options.RabbitMq.ManagementPort = value);
        ApplyResolvedString(configuration, "RABBITMQ_USERNAME", value => options.RabbitMq.Username = value);
        ApplyResolvedString(configuration, "RABBITMQ_PASSWORD", value => options.RabbitMq.Password = value);
    }

    private static void ApplyResolvedString(
        IConfiguration configuration,
        string suffix,
        Action<string> apply)
    {
        var value = LegacyEnvironmentVariables.GetPrefixed(suffix)
            ?? configuration[$"NEVAXIS_ZENTRA_{suffix}"]
            ?? configuration[$"NEVAXIS_FACTOS_{suffix}"]
            ?? configuration[$"CREVAFACTOS_{suffix}"];

        if (value is not null)
        {
            apply(value);
        }
    }

    private static void ApplyResolvedInt(
        IConfiguration configuration,
        string suffix,
        Action<int> apply)
    {
        var value = LegacyEnvironmentVariables.GetPrefixed(suffix)
            ?? configuration[$"NEVAXIS_ZENTRA_{suffix}"]
            ?? configuration[$"NEVAXIS_FACTOS_{suffix}"]
            ?? configuration[$"CREVAFACTOS_{suffix}"];

        if (value is null)
        {
            return;
        }

        if (!int.TryParse(value, out var parsed))
        {
            throw new OptionsValidationException(
                InfrastructureOptions.SectionName,
                typeof(InfrastructureOptions),
                [$"Environment/configuration suffix '{suffix}' must be an integer."]);
        }

        apply(parsed);
    }

    private static bool IsValid(PostgreSqlEndpointOptions options) =>
        !string.IsNullOrWhiteSpace(options.Host)
        && IsValidPort(options.Port)
        && !string.IsNullOrWhiteSpace(options.Database)
        && !string.IsNullOrWhiteSpace(options.Username)
        && !string.IsNullOrWhiteSpace(options.Password);

    private static bool IsValid(RabbitMqEndpointOptions options) =>
        !string.IsNullOrWhiteSpace(options.Host)
        && IsValidPort(options.AmqpPort)
        && IsValidPort(options.ManagementPort)
        && !string.IsNullOrWhiteSpace(options.Username)
        && !string.IsNullOrWhiteSpace(options.Password);

    private static bool IsValidPort(int port) => port is > 0 and <= 65535;
}
