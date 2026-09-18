namespace NevaxisZentra.Persistence.ThroughputIntelligence;

public sealed class ThroughputIntelligenceException : Exception
{
    public ThroughputIntelligenceException(string failureCode, string message)
        : base(message)
    {
        FailureCode = failureCode;
    }

    public string FailureCode { get; }
}
