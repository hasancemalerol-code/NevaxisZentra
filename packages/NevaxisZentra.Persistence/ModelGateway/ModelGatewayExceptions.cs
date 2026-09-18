namespace NevaxisZentra.Persistence.ModelGateway;

public sealed class ModelGatewayException : Exception
{
    public string FailureCode { get; }

    public ModelGatewayException(string failureCode, string message) : base(message) =>
        FailureCode = failureCode;
}
