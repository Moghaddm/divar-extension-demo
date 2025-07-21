using System.ClientModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using DivarExtensionDemo.Constants;
using DivarExtensionDemo.Models.Comparision;
using DivarExtensionDemo.Models.Divar;
using Microsoft.AspNetCore.Mvc;
using OpenAI;
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

app.MapGet("/goto-divar", ([FromQuery] string postToken) =>
{
    var clientId = builder.Configuration.GetSection("Divar:Extension:ClientId").Value!;
    var scope = $"USER_PHONE";
    var queries = string.Join('&', new Dictionary<string, string>
    {
        { "response_type", "code" },
        { "redirect_uri", "http://localhost:5053/auth/fallback" },
        { "client_id", clientId },
        { "scope", scope },
        { "state", "@Mohammad2005" }
    }.Select(q => $"{q.Key}={q.Value}"));
    var redirectUrl = DivarConstants.AuthorizationRequestUrl + "?" + queries;
    return Results.Redirect(redirectUrl);
});

app.MapGet("/auth/fallback", async (
    [FromServices] IHttpClientFactory httpClientFactory,
    [FromQuery] string state,
    [FromQuery] string code,
    CancellationToken cancellationToken
) =>
{
    const string originalState = "@Mohammad2005";
    if (originalState != state) return Results.Unauthorized();
    var client = httpClientFactory.CreateClient();
    var request = new HttpRequestMessage(HttpMethod.Post, DivarConstants.AccessTokenRequestUrl);
    request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        { "grant_type", "authorization_code" },
        { "code", code },
        { "client_id", builder.Configuration.GetSection("Divar:Extension:ClientId").Value! },
        { "client_secret", builder.Configuration.GetSection("Divar:Extension:ClientSecret").Value! },
        { "redirect_uri", "http://localhost:5053/auth/fallback" }
    });
    var clientResponse = await client.SendAsync(request, cancellationToken);
    clientResponse.EnsureSuccessStatusCode();
    if (!clientResponse.IsSuccessStatusCode) return Results.Unauthorized();
    var responseAsText = await clientResponse.Content.ReadAsStringAsync(cancellationToken);
    var response = JsonSerializer.Deserialize<AccessTokenResponse>(responseAsText, new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    });
    var redirectUrl = $"{DivarConstants.BaseAppUrl}?token={response!.AccessToken}";
    return Results.Redirect(redirectUrl);
});

app.MapGet("/comparasion", async ([FromServices] IHttpClientFactory httpClientFactory, [FromQuery] string postToken,
    CancellationToken cancellationToken) =>
{
    var divarApiKey = builder.Configuration.GetSection("Divar:Extension:ApiKey").Value!;
    var aiApiKey = builder.Configuration.GetSection("AI:ApiKey").Value!;
    var aiEndPoint = builder.Configuration.GetSection("AI:EndPoint").Value!;
    var client = new HttpClient();
    var request = new HttpRequestMessage(HttpMethod.Get, DivarConstants.RetrievePostInformationUrl + postToken);
    request.Headers.Add("Accept", "application/json");
    request.Headers.Add("X-API-Key", divarApiKey);
    var clientResponse = await client.SendAsync(request, cancellationToken);
    if (!clientResponse.IsSuccessStatusCode) return Results.BadRequest("Request to retrieve post information failed.");
    var responseAsJson = await clientResponse.Content.ReadAsStringAsync(cancellationToken);
    List<string> items = ["Red Dead Redemption 2", "Adobe Premiere 2024", "After Effects 2024"];
    var prompt = AiConstants.BaseComparisionPrompt +
                 "\nPost: \n" + responseAsJson +
                 "\nItems: \n" + string.Join(",", items);
    var aiClient = new OpenAIClient(new ApiKeyCredential(aiApiKey),
        new OpenAIClientOptions { Endpoint = new Uri(aiEndPoint) });
    var aiResponse = await aiClient.GetChatClient(AiConstants.DefaultCompletionModel).CompleteChatAsync(prompt);
    var response = JsonSerializer.Deserialize<ComparisionVm>(aiResponse.Value.Content[0].Text);
    return Results.Ok(response);
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