using Microsoft.AspNetCore.Authorization;

namespace NevaxisZentra.Api.Authorization;

public sealed record PermissionRequirement(string PermissionKey) : IAuthorizationRequirement;
