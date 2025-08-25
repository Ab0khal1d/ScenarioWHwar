using System.Reflection;
using ScenariosWHwar.Discovery.API.Host.Extensions;
using ScenariosWHwar.Discovery.API.Host;
using ScenariosWHwar.API.Core.Host;
using ScenariosWHwar.API.Core.Host.Extensions;

var appAssembly = Assembly.GetExecutingAssembly();
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCustomProblemDetails();

builder.AddWebApi();
builder.AddApplication();


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
app.MapHealthChecks("/health");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.RegisterEndpoints(appAssembly);

app.UseExceptionHandler();

app.Run();

namespace ScenariosWHwar.Discovery.API
{
    public partial class Program;
}
