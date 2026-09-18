namespace NevaxisZentra.Configuration;

public sealed class InfrastructureOptions
{
    public const string SectionName = "Infrastructure";

    public PostgreSqlEndpointOptions PostgreSql { get; set; } = new();

    public RabbitMqEndpointOptions RabbitMq { get; set; } = new();
}

public sealed class PostgreSqlEndpointOptions
{
    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 5432;

    public string Database { get; set; } = "nevaxis_zentra";

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public sealed class RabbitMqEndpointOptions
{
    public string Host { get; set; } = "localhost";

    public int AmqpPort { get; set; } = 5672;

    public int ManagementPort { get; set; } = 15672;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
