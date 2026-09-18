namespace NevaxisZentra.Configuration;

[Flags]
public enum InfrastructureDependency
{
    None = 0,
    PostgreSql = 1,
    RabbitMq = 2,
}
