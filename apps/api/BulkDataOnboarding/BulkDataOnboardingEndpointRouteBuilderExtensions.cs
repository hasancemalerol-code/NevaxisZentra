using Microsoft.AspNetCore.Mvc;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.BulkDataOnboarding;

namespace NevaxisZentra.Api.BulkDataOnboarding;

public static class BulkDataOnboardingEndpointRouteBuilderExtensions
{
    private static readonly string TemplateRoot = Path.Combine(
        AppContext.BaseDirectory,
        "customer-import-templates");

    public static IServiceCollection AddNevaxisZentraBulkDataOnboardingApi(this IServiceCollection services) =>
        services.AddNevaxisZentraBulkDataOnboarding();

    public static IEndpointRouteBuilder MapBulkDataOnboardingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/bulk-data-onboarding").RequireAuthorization();

        group.MapGet("/jobs", ListJobsAsync);
        group.MapPost("/jobs", CreateJobAsync);
        group.MapGet("/jobs/{jobId:guid}", GetWorkspaceAsync);
        group.MapPost("/jobs/{jobId:guid}/files", UploadFilesAsync);
        group.MapPost("/jobs/{jobId:guid}/discover", DiscoverAsync);
        group.MapPost("/jobs/{jobId:guid}/mapping/auto", ApplyAutoMappingAsync);
        group.MapPost("/jobs/{jobId:guid}/stage-validate", StageValidateAsync);
        group.MapPost("/jobs/{jobId:guid}/dry-run", DryRunAsync);
        group.MapPost("/jobs/{jobId:guid}/commit", CommitAsync);
        group.MapPost("/jobs/{jobId:guid}/rollback", RollbackAsync);
        group.MapGet("/mapping-profiles", ListMappingProfilesAsync);
        group.MapPut("/mapping-profiles/{profileId:guid?}", UpsertMappingProfileAsync);
        group.MapGet("/templates/{templateName}", DownloadTemplateAsync);

        return endpoints;
    }

    private static async Task<IResult> ListJobsAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IBulkDataOnboardingService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, BulkDataOnboardingPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var jobs = await service.ListJobsAsync(
            new BulkDataOperationContext(access.TenantId, access.FacilityId, access.IdentitySubjectId.ToString()),
            cancellationToken);
        return TypedResults.Ok(jobs);
    }

    private static async Task<IResult> CreateJobAsync(
        [FromBody] CreateBulkImportJobRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IBulkDataOnboardingService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, BulkDataOnboardingPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var job = await service.CreateJobAsync(
            new BulkDataOperationContext(access.TenantId, access.FacilityId, access.IdentitySubjectId.ToString()),
            request,
            cancellationToken);
        return TypedResults.Ok(job);
    }

    private static async Task<IResult> GetWorkspaceAsync(
        Guid jobId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IBulkDataOnboardingService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, BulkDataOnboardingPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var workspace = await service.GetWorkspaceAsync(
            new BulkDataOperationContext(access.TenantId, access.FacilityId, access.IdentitySubjectId.ToString()),
            jobId,
            cancellationToken);
        return TypedResults.Ok(workspace);
    }

    private static async Task<IResult> UploadFilesAsync(
        Guid jobId,
        [FromQuery] Guid? facilityId,
        HttpRequest request,
        IFacilityAccessResolver facilityAccessResolver,
        IBulkDataOnboardingService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, BulkDataOnboardingPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        if (!request.HasFormContentType)
        {
            return TypedResults.BadRequest(new { code = BulkFailureCodes.ValidationFailed, message = "Multipart form expected." });
        }

        var uploads = new List<BulkImportUploadFile>();
        foreach (var file in request.Form.Files)
        {
            uploads.Add(new BulkImportUploadFile(
                file.FileName,
                file.OpenReadStream(),
                file.Length,
                file.ContentType ?? "application/octet-stream"));
        }

        if (uploads.Count == 0)
        {
            return TypedResults.BadRequest(new { code = BulkFailureCodes.ValidationFailed, message = "No files uploaded." });
        }

        try
        {
            var result = await service.UploadFilesAsync(
                new BulkDataOperationContext(access.TenantId, access.FacilityId, access.IdentitySubjectId.ToString()),
                jobId,
                uploads,
                cancellationToken);
            return TypedResults.Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith(BulkFailureCodes.FileRejected, StringComparison.Ordinal))
        {
            return TypedResults.BadRequest(new { code = BulkFailureCodes.FileRejected, message = ex.Message.Split(':').LastOrDefault()?.Trim() });
        }
    }

    private static async Task<IResult> DiscoverAsync(
        Guid jobId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IBulkDataOnboardingService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, BulkDataOnboardingPermissionKeys.Run, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var job = await service.DiscoverAsync(
            new BulkDataOperationContext(access.TenantId, access.FacilityId, access.IdentitySubjectId.ToString()),
            jobId,
            cancellationToken);
        return TypedResults.Ok(job);
    }

    private static async Task<IResult> ApplyAutoMappingAsync(
        Guid jobId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IBulkDataOnboardingService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, BulkDataOnboardingPermissionKeys.MappingManage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var job = await service.ApplyAutoMappingAsync(
            new BulkDataOperationContext(access.TenantId, access.FacilityId, access.IdentitySubjectId.ToString()),
            jobId,
            cancellationToken);
        return TypedResults.Ok(job);
    }

    private static async Task<IResult> StageValidateAsync(
        Guid jobId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IBulkDataOnboardingService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, BulkDataOnboardingPermissionKeys.Run, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var job = await service.StageAndValidateAsync(
            new BulkDataOperationContext(access.TenantId, access.FacilityId, access.IdentitySubjectId.ToString()),
            jobId,
            cancellationToken);
        return TypedResults.Ok(job);
    }

    private static async Task<IResult> DryRunAsync(
        Guid jobId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IBulkDataOnboardingService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, BulkDataOnboardingPermissionKeys.Run, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var result = await service.DryRunAsync(
            new BulkDataOperationContext(access.TenantId, access.FacilityId, access.IdentitySubjectId.ToString()),
            jobId,
            cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<IResult> CommitAsync(
        Guid jobId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IBulkDataOnboardingService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, BulkDataOnboardingPermissionKeys.Commit, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var job = await service.CommitAsync(
            new BulkDataOperationContext(access.TenantId, access.FacilityId, access.IdentitySubjectId.ToString()),
            jobId,
            cancellationToken);
        return TypedResults.Ok(job);
    }

    private static async Task<IResult> RollbackAsync(
        Guid jobId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IBulkDataOnboardingService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, BulkDataOnboardingPermissionKeys.Manage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        try
        {
            var job = await service.RollbackAsync(
                new BulkDataOperationContext(access.TenantId, access.FacilityId, access.IdentitySubjectId.ToString()),
                jobId,
                cancellationToken);
            return TypedResults.Ok(job);
        }
        catch (InvalidOperationException ex) when (ex.Message == BulkFailureCodes.RollbackBlocked)
        {
            return TypedResults.Conflict(new { code = BulkFailureCodes.RollbackBlocked });
        }
    }

    private static async Task<IResult> ListMappingProfilesAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IBulkDataOnboardingService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, BulkDataOnboardingPermissionKeys.Read, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var profiles = await service.ListMappingProfilesAsync(
            new BulkDataOperationContext(access.TenantId, access.FacilityId, access.IdentitySubjectId.ToString()),
            cancellationToken);
        return TypedResults.Ok(profiles);
    }

    private static async Task<IResult> UpsertMappingProfileAsync(
        Guid? profileId,
        [FromBody] UpsertBulkMappingProfileRequest request,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        IBulkDataOnboardingService service,
        CancellationToken cancellationToken)
    {
        var (access, failure) = await ResolveAccessAsync(
            facilityId, BulkDataOnboardingPermissionKeys.MappingManage, facilityAccessResolver, cancellationToken);
        if (access is null) return failure!;

        var profile = await service.UpsertMappingProfileAsync(
            new BulkDataOperationContext(access.TenantId, access.FacilityId, access.IdentitySubjectId.ToString()),
            profileId,
            request,
            cancellationToken);
        return TypedResults.Ok(profile);
    }

    private static IResult DownloadTemplateAsync(string templateName)
    {
        var safeName = Path.GetFileName(templateName);
        var repoRoot = ResolveRepositoryTemplateRoot();
        var path = Path.Combine(repoRoot, safeName);
        if (!File.Exists(path))
        {
            path = Path.Combine(TemplateRoot, safeName);
        }

        if (!File.Exists(path))
        {
            return TypedResults.NotFound();
        }

        var contentType = safeName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            : "text/csv";
        return TypedResults.File(File.OpenRead(path), contentType, safeName);
    }

    private static string ResolveRepositoryTemplateRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "release", "customer-import-templates");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return TemplateRoot;
    }

    private static async Task<(FacilityAccessContext? Access, IResult? Failure)> ResolveAccessAsync(
        Guid? facilityId,
        string permissionKey,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, TypedResults.BadRequest(new { code = "facility-missing", message = "Tesis tanımlayıcısı zorunludur." }));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, permissionKey),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, TypedResults.Forbid());
        }

        return (accessResult.Access, null);
    }
}
