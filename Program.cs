using System.ClientModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using DivarExtensionDemo.Constants;
using DivarExtensionDemo.Entities;
using DivarExtensionDemo.Infrastructure.Divar.Models;
using DivarExtensionDemo.Models.Comparision;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OpenAI;
using Scalar.AspNetCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

var redisConfiguration = builder.Configuration.GetSection("Redis:Address").Value!;
await ConnectionMultiplexer.ConnectAsync(redisConfiguration);
builder.Services.AddSingleton<IConnectionMultiplexer>();
builder.Services.AddSingleton<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// App routes

const string fallbackUrl = "http://localhost:5053/auth/fallback";
const string statesCacheKey = "States";

app.MapGet("/", async ([FromQuery] string postToken, [FromServices] IDatabase database) =>
{
    var clientId = builder.Configuration.GetSection("Divar:Extension:ClientId").Value!;
    const string scopes = "USER_PHONE";
    var states = await database.ListRangeAsync(statesCacheKey);
    var lastStateIdentifier = states.OrderDescending().FirstOrDefault();
    var state = lastStateIdentifier + 1;
    var queries = string.Join('&', new Dictionary<string, string>
    {
        { "response_type", "code" },
        { "redirect_uri", fallbackUrl },
        { "client_id", clientId },
        { "scope", scopes },
        { "state", state }
    }.Select(q => $"{q.Key}={q.Value}"));
    var redirectUrl = DivarConstants.AuthorizationRequestUrl + "?" + queries;
    return Results.Redirect(redirectUrl);
});

app.MapGet("/auth/fallback", async ([FromQuery] string state,
    [FromQuery] string code, [FromQuery] string postToken, [FromServices] IHttpClientFactory httpClientFactory,
    IDatabase database,
    CancellationToken cancellationToken) =>
{
    var states = await database.ListRangeAsync(statesCacheKey);
    if (states.All(s => s != state)) return Results.Unauthorized();

    var request = new HttpRequestMessage(HttpMethod.Post, DivarConstants.AccessTokenRequestUrl);
    var clientId = builder.Configuration.GetSection("Divar:Extension:ClientId").Value!;
    var clientSecret = builder.Configuration.GetSection("Divar:Extension:ClientSecret").Value!;
    request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        { "grant_type", "authorization_code" },
        { "code", code },
        { "client_id", clientId },
        { "client_secret", clientSecret },
        { "redirect_uri", fallbackUrl }
    });

    var client = httpClientFactory.CreateClient();
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

    var redirectUrl = $"{DivarConstants.BaseAppUrl}?token={response!.AccessToken}&postToken={postToken}";
    return Results.Redirect(redirectUrl);
});

app.MapPost("/comparasion", async ([FromServices] IHttpClientFactory httpClientFactory, [FromQuery] string postToken,
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

app.MapGet("/comparision/{id}", async ([FromRoute] string id, [FromServices] IMongoDatabase mongoDatabase,
    CancellationToken cancellationToken) =>
{
    var collection = mongoDatabase.GetCollection<Comparision>("Comparisions");
    var filterDefinition = Builders<Comparision>.Filter.Eq("_id", id);
    var comparision = await collection.Find(filterDefinition).FirstOrDefaultAsync(cancellationToken);
    if (comparision == null) return Results.NotFound();
    var comparisionVm = new ComparisionVm
    {
        Advice = comparision.Advice,
        NegativeConclusion = comparision.NegativeConclusion,
        PositiveConclusion = comparision.PositiveConclusion,
        Softwares = comparision.Softwares.Select(s => new ComparisionSoftwareVm
        {
            Name = s.Name,
            Percentage = s.Percentage,
            Status = s.Status
        }).ToList()
    };
    return Results.Ok(comparisionVm);
});

app.Run();