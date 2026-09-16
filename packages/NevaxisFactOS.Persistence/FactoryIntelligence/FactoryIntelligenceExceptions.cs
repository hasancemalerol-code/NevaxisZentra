namespace NevaxisFactOS.Persistence.FactoryIntelligence;

public sealed class FactoryIntelligenceException : Exception
{
    public FactoryIntelligenceException(string failureCode, string message) : base(message)
    {
        FailureCode = failureCode;
    }

    public string FailureCode { get; }
}
