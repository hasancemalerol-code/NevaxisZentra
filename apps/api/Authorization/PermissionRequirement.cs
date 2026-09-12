using Microsoft.AspNetCore.Authorization;

namespace NevaxisFactOS.Api.Authorization;

public sealed record PermissionRequirement(string PermissionKey) : IAuthorizationRequirement;
