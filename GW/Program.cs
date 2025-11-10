using GW.Constants;
using GW.Middleware;
using GW.Services;
using Microsoft.AspNetCore.Authentication;
using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Polly;


var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
    .AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddOcelot(builder.Configuration).AddConsul<ConsulService>().AddConfigStoredInConsul().AddCacheManager(x =>
{
    x.WithDictionaryHandle();
}).AddPolly().AddDelegatingHandler<CustomHeaderDelegatingHandler>(true);


builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, AuthMiddleware>(Auth.ApiKeyScheme, options => { });
// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseOcelot(
new OcelotPipelineConfiguration
{
    AuthorizationMiddleware = async (ctx, next) =>
    {
        // Custom authorization logic can be added here
        await next.Invoke();
    }
}
).Wait();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();

