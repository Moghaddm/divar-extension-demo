using DivarExtensionDemo;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// App routes

app.MapGet("/", ([FromServices] IHttpClientFactory httpClientFactory, [FromQuery] string posToken) =>
{
    var clientId = builder.Configuration.GetSection("Divar:App:ClientId").ToString()!;
    var scope = $"POST_ADDON_CREATE.{posToken}";
    var queries = string.Join('&', new Dictionary<string, string>
    {
        { "response_type", "code" },
        { "client_id", clientId },
        { "scope", scope },
        { "state", "111" }
    });
    var redirectUrl = DivarConstants.AuthorizationRequestUrl + "?" + queries;
    return Task.FromResult(Results.Redirect(redirectUrl));
});

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