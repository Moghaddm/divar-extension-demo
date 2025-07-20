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
    var response = JsonSerializer.Deserialize<AccessTokenResponse>(responseAsText);
    var redirectUrl = $"{DivarConstants.BaseAppUrl}?token={response!.Access_Token}";
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
    var post = JsonSerializer.Deserialize<PostResponse>(responseAsJson,
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower })!;
    List<string> items = ["Red Dead Redemption 2", "Adobe Premiere 2024", "After Effects 2024"];
    var prompt =
        """
            I will provide you with information about a technological or digital product post from an Iranian e-commerce platform called Divar. The product can only be a laptop, PC, or mobile phone.
            Your task is to:
            1. Analyze the hardware specifications in the product post.
            2. Compare the given hardware against the system requirements of each game or software I provide.
            3. Return a comparison result that shows how well the device can run each item. Represent this as a percentage (0–100%), indicating performance capability. For example: ""Premiere 2024"": 65%
            Your response must be:
            - In Persian (Farsi) language.
            - In the form of a JSON object that can be easily deserialized into the following C# POCO model:
            public sealed class ComparisionResponse
            {
                public string Text { get; init; } = null!;
                public Dictionary<string, float> Items { get; init; } = null!;
            }
            Use `Text` to provide a short summary in Persian, such as whether the system is generally good or weak for games/software.
            Use `Items` to list each game/software name (in English, exactly as I provide them) and its compatibility percentage.
            Example expected output (in JSON):
            {
              ""Text"": ""این گوشی برای اجرای نرم‌افزارهای سنگین مناسب نیست."",
              ""Items"": {
                ""Premiere 2024"": 30.0,
                ""After Effects 2024"": 20.0,
                ""Red Dead Redemption 2"": 10.0
              }
            }
            Only reply with the JSON object and nothing else. Just the json without ``` like things or code additional styling.
            Now, I will give you the product post and list of items to compare.
            Post Information:
        """ + "\n" +
        JsonSerializer.Serialize(post,
            new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }) + "\n Items: \n" +
        string.Join(",", items);
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