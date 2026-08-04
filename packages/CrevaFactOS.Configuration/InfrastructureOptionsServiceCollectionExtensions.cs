using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CrevaFactOS.Configuration;

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
        ApplyString(configuration, "CREVAFACTOS_POSTGRES_HOST", value => options.PostgreSql.Host = value);
        ApplyInt(configuration, "CREVAFACTOS_POSTGRES_PORT", value => options.PostgreSql.Port = value);
        ApplyString(configuration, "CREVAFACTOS_POSTGRES_DATABASE", value => options.PostgreSql.Database = value);
        ApplyString(configuration, "CREVAFACTOS_POSTGRES_USERNAME", value => options.PostgreSql.Username = value);
        ApplyString(configuration, "CREVAFACTOS_POSTGRES_PASSWORD", value => options.PostgreSql.Password = value);

        ApplyString(configuration, "CREVAFACTOS_RABBITMQ_HOST", value => options.RabbitMq.Host = value);
        ApplyInt(configuration, "CREVAFACTOS_RABBITMQ_AMQP_PORT", value => options.RabbitMq.AmqpPort = value);
        ApplyInt(
            configuration,
            "CREVAFACTOS_RABBITMQ_MANAGEMENT_PORT",
            value => options.RabbitMq.ManagementPort = value);
        ApplyString(configuration, "CREVAFACTOS_RABBITMQ_USERNAME", value => options.RabbitMq.Username = value);
        ApplyString(configuration, "CREVAFACTOS_RABBITMQ_PASSWORD", value => options.RabbitMq.Password = value);
    }

    private static void ApplyString(
        IConfiguration configuration,
        string key,
        Action<string> apply)
    {
        var value = configuration[key];
        if (value is not null)
        {
            apply(value);
        }
    }

    private static void ApplyInt(
        IConfiguration configuration,
        string key,
        Action<int> apply)
    {
        var value = configuration[key];
        if (value is null)
        {
            return;
        }

        if (!int.TryParse(value, out var parsed))
        {
            throw new OptionsValidationException(
                InfrastructureOptions.SectionName,
                typeof(InfrastructureOptions),
                [$"Configuration value '{key}' must be an integer."]);
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
