namespace NevaxisZentra.Persistence.RealFactoryPilot;

public sealed class RealFactoryPilotException : Exception
{
    public string FailureCode { get; }

    public RealFactoryPilotException(string failureCode, string message)
        : base(message)
    {
        FailureCode = failureCode;
    }
}
