using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace ScenariosWHwar.API.Core.Common.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string? UserId => httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
