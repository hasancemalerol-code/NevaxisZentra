using Microsoft.AspNetCore.Authorization;

namespace CrevaFactOS.Api.Authorization;

public sealed record PermissionRequirement(string PermissionKey) : IAuthorizationRequirement;
