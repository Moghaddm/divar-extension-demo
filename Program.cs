using System.Security.Claims;
using DivarExtensionDemo.Extensions;
using DivarExtensionDemo.Infrastructure.JWT;
using DivarExtensionDemo.Infrastructure.MongoDb;
using DivarExtensionDemo.Services;
using DivarExtensionDemo.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var configuration = builder.Configuration;
builder.Services
    .AddOpenAi(configuration)
    .AddMongoDb(configuration)
    .AddAuth(configuration)
    .AddRedis(configuration);

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ISoftwareService, SoftwareService>();
builder.Services.AddScoped<IComparisionService, ComparisionService>();
builder.Services.AddScoped<IDivarService, DivarService>();

builder.Services.AddOpenApi();
builder.Services.AddHttpClient();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    await app.DataSeedAsync();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/auth", async (
    [FromQuery] string postToken,
    [FromServices] IDivarService divarService,
    CancellationToken cancellationToken
) =>
{
    var divarAuthRequestUrl = await divarService.GenerateSsoAuthUrlAsync(postToken, cancellationToken);
    return Results.Ok(divarAuthRequestUrl);
});

app.MapGet("/auth/token", async (
    [FromQuery] string state,
    [FromQuery] string code,
    [FromQuery] string postToken,
    [FromServices] IDivarService divarService,
    CancellationToken cancellationToken
) =>
{
    var divarAccessToken = await divarService.HandleFallbackAsync(state, code, cancellationToken);

    var token = JwtFactory.CreateAccessToken(
        builder.Configuration,
        [new Claim("DivarAccessToken", divarAccessToken), new Claim("PostToken", postToken)],
        "Comparision"
    );

    return Results.Ok(token);
});

app.MapGet("/softwares", ([FromServices] ISoftwareService softwareService, CancellationToken cancellationToken) =>
{
    var names = softwareService.GetNamesAsync(cancellationToken);
    return Results.Ok(names);
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Comparision" });

app.MapPost("/comparasion", async (
    [FromServices] IComparisionService comparisionService,
    [FromServices] IHttpContextAccessor httpContextAccessor,
    [FromBody] string[] softwareIds,
    CancellationToken cancellationToken
) =>
{
    var postToken = ((ClaimsIdentity)httpContextAccessor.HttpContext!.User.Identity!).FindFirst("PostToken")!.Value;
    var divarAccessToken = ((ClaimsIdentity)httpContextAccessor.HttpContext!.User.Identity!)
        .FindFirst("DivarAccessToken")!.Value;

    var comparisionId =
        await comparisionService.CreateAsync(postToken, softwareIds, divarAccessToken, cancellationToken);

    return Results.Ok(comparisionId);
}).RequireAuthorization(new AuthorizeAttribute { Roles = "Comparision" });

app.MapGet("/comparision/{id}", async (
    [FromRoute] string id,
    [FromServices] IComparisionService service,
    CancellationToken cancellationToken
) =>
{
    var comparision = await service.GetAsync(id, cancellationToken);
    return Results.Ok(comparision);
});

app.Run();