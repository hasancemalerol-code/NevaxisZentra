namespace NevaxisZentra.Persistence.ContextKnowledge;

public sealed class ContextKnowledgeException : Exception
{
    public string FailureCode { get; }

    public ContextKnowledgeException(string failureCode, string message)
        : base(message)
    {
        FailureCode = failureCode;
    }
}
