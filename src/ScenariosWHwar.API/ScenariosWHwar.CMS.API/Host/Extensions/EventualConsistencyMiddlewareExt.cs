using ScenariosWHwar.CMS.API.Common.Middleware;

namespace ScenariosWHwar.CMS.API.Host.Extensions;

public static class EventualConsistencyMiddlewareExt
{
    public static IApplicationBuilder UseEventualConsistencyMiddleware(this IApplicationBuilder app)
    {
        app.UseMiddleware<EventualConsistencyMiddleware>();
        return app;
    }
}
