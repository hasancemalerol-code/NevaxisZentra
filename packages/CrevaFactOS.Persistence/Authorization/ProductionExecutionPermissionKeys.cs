namespace CrevaFactOS.Persistence.Authorization;

public static class ProductionExecutionPermissionKeys
{
    public const string Read = "production.execution.read";
    public const string Write = "production.execution.write";
    public const string Start = "production.execution.start";
    public const string Pause = "production.execution.pause";
    public const string Complete = "production.execution.complete";
    public const string Record = "production.execution.record";

    // 3L
    public const string Downtime = "production.execution.downtime";
    public const string Setup = "production.execution.setup";

    // 4L
    public const string Exception = "production.execution.exception";
}
