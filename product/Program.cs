using Consul;
using System.Net;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IConsulClient, ConsulClient>(p => new ConsulClient(consulConfig =>
{
    consulConfig.Address = new Uri("http://consul:8500");
}));
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

var consulClient = app.Services.GetRequiredService<IConsulClient>();

var hostname = Dns.GetHostName();
var ip = Dns.GetHostEntry(hostname)
            .AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork)
            .ToString();

var registration = new AgentServiceRegistration()
{
    ID = $"{app.Environment.ApplicationName}-{Guid.NewGuid()}",
    Name = "product",
    Address = ip,
    Port = 5298,
    Tags = new[] { "api" },
    Check = new AgentServiceCheck()
    {
        HTTP = $"http://{ip}:5298/health",  // Health check endpoint
        Interval = TimeSpan.FromSeconds(10),    // Check every 10s
        Timeout = TimeSpan.FromSeconds(5),      // Fail if no response in 5s
        DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1) // Remove if down too long
    }
};

await consulClient.Agent.ServiceRegister(registration, CancellationToken.None);

var lifetime = app.Lifetime; // IHostApplicationLifetime
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.MapHealthChecks("/health");
app.UseHttpsRedirection();
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
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
