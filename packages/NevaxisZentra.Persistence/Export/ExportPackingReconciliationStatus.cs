namespace NevaxisZentra.Persistence.Export;

public enum ExportPackingReconciliationStatus
{
    NotStarted,
    Incomplete,
    Matched,
    Overpacked,
    Mismatch,
    UnableToValidate,
}
