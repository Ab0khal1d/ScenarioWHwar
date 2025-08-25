using ScenariosWHwar.API.Core.Host;
using ScenariosWHwar.API.Core.Host.Extensions;
using ScenariosWHwar.CMS.API.Host;
using System.Reflection;

var appAssembly = Assembly.GetExecutingAssembly();
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomProblemDetails();

builder.AddWebApi();
builder.AddApplication();
builder.AddInfrastructure();

builder.Services.ConfigureFeatures(builder.Configuration, appAssembly);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.MapOpenApi();
app.MapCustomScalarApiReference();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.RegisterEndpoints(appAssembly);
app.UseEventualConsistencyMiddleware();

app.UseExceptionHandler();

app.Run();

namespace ScenariosWHwar.CMS.API
{
    public partial class Program;
}
