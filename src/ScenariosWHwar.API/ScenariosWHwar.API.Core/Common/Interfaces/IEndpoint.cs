using Microsoft.AspNetCore.Routing;

namespace ScenariosWHwar.API.Core.Common.Interfaces;

public interface IEndpoint
{
    static abstract void MapEndpoint(IEndpointRouteBuilder endpoints);
}
