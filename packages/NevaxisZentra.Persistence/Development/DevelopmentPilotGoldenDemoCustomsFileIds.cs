namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotGoldenDemoCustomsFileIds
{
    public static readonly Guid ReadPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000f7d");
    public static readonly Guid ManagePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000f7e");
    public static readonly Guid ApprovePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000f7f");
    public static readonly Guid ExecutePermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000f80");
    public static readonly Guid VerifyPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000f81");
    public static readonly Guid ResetPermissionDefinitionId =
        Guid.Parse("10000000-0000-4000-8000-000000000f82");

    public static readonly Guid OutcomeId =
        Guid.Parse("cc000001-0010-4000-8000-000000000001");
    public static readonly Guid CommercialInvoiceDocumentId =
        Guid.Parse("cc000001-0020-4000-8000-000000000001");
    public static readonly Guid PackingListDocumentId =
        Guid.Parse("cc000001-0020-4000-8000-000000000002");
}
