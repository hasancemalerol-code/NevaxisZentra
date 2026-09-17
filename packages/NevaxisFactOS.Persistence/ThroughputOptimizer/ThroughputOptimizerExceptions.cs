namespace NevaxisFactOS.Persistence.ThroughputOptimizer;

public sealed class ThroughputOptimizerException : Exception
{
    public string FailureCode { get; }

    public ThroughputOptimizerException(string failureCode, string message) : base(message) =>
        FailureCode = failureCode;
}
