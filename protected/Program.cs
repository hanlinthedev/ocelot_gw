using System.Net;
using System.Net.Sockets;
using Consul;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
{
    consulConfig.Address = new Uri("http://consul:8500");
}));
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();


var consulClient = app.Services.GetRequiredService<IConsulClient>();

var hostname = Dns.GetHostName();
var ip = Dns.GetHostEntry(hostname)
            .AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork)
            .ToString();

var registration = new AgentServiceRegistration()
{
    ID = $"{app.Environment.ApplicationName}-{Guid.NewGuid()}",
    Name = "prrotected",
    Address = ip,
    Port = 5253,
    Tags = new[] { "api" },
    Check = new AgentServiceCheck()
    {
        HTTP = $"http://{ip}:5253/health",
        Interval = TimeSpan.FromSeconds(10),
        Timeout = TimeSpan.FromSeconds(5),
        DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
    }
};

await consulClient.Agent.ServiceRegister(registration, CancellationToken.None);

var lifetime = app.Lifetime;
lifetime.ApplicationStopping.Register(async () =>
{
    try
    {
        await consulClient.Agent.ServiceDeregister(registration.ID, CancellationToken.None);
        Console.WriteLine($"Service {registration.ID} deregistered from Consul.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error deregistering service: {ex.Message}");
    }
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapHealthChecks("/health");
app.UseHttpsRedirection();

var summaries = new[]
{
    "Protected", "Protected", "Protected", "Protected", "Protected", "Protected", "Protected", "Protected", "Protected", "Protected"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
