using System.Text.Json;
using DivarExtensionDemo.Constants;
using DivarExtensionDemo.Models;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

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
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// App routes

app.MapGet("/a", ([FromQuery] string postToken) =>
{
    var clientId = builder.Configuration.GetSection("Divar:App:ClientId").ToString()!;
    var scope = $"POST_ADDON_CREATE.{postToken}";
    var queries = string.Join('&', new Dictionary<string, string>
    {
        { "response_type", "code" },
        { "client_id", clientId },
        { "scope", scope },
        { "state", "111" }
    });
    var redirectUrl = DivarConstants.AuthorizationRequestUrl + "?" + queries;
    return Results.Redirect(redirectUrl);
});

app.MapPost("/auth/fallback", async (
    [FromServices] IHttpClientFactory httpClientFactory,
    [FromQuery] string state,
    [FromQuery] string code,
    CancellationToken cancellationToken
) =>
{
    const string originalState = "111";
    if (originalState != state) return Results.Unauthorized();
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Post, DivarConstants.AccessTokenRequestUrl);
    request.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
    request.Content = JsonContent.Create(new
    {
        grant_type = "authorization_code",
        Code = code,
        client_id = builder.Configuration.GetSection("Divar:App:ClientId").ToString()!,
        client_secret = builder.Configuration.GetSection("Divar:App:ClientSecret").ToString()!,
        redirect_uri = DivarConstants.AuthorizationRequestUrl
    });
    var clientResponse = await client.SendAsync(request, cancellationToken);
    clientResponse.EnsureSuccessStatusCode();
    if (!clientResponse.IsSuccessStatusCode) return Results.Unauthorized();
    var responseAsText = await clientResponse.Content.ReadAsStringAsync(cancellationToken);
    var response = JsonSerializer.Deserialize<AuthAccessTokenResponse>(responseAsText);
    var redirectUrl = $"{DivarConstants.BaseAppUrl}?token={response!.Access_Token}";
    return Results.Redirect(redirectUrl);
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