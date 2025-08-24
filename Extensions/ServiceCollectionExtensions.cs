using System.ClientModel;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using OpenAI;
using StackExchange.Redis;

namespace DivarExtensionDemo.Extensions;

internal static class ServiceCollectionExtensions
{
    internal static void AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(configuration.GetSection("Redis:Address").Value!));
        services.AddSingleton<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
    }

    internal static IServiceCollection AddOpenAi(this IServiceCollection services, IConfiguration configuration)
    {
        var aiApiKey = configuration.GetSection("AI:ApiKey").Value!;
        var aiEndPoint = configuration.GetSection("AI:EndPoint").Value!;

        services.AddSingleton<OpenAIClient>(_ => new OpenAIClient(new ApiKeyCredential(aiApiKey),
            new OpenAIClientOptions { Endpoint = new Uri(aiEndPoint) }));

        return services;
    }

    internal static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration.GetSection("JWT:Key").Value!;
        var issuer = configuration.GetSection("JWT:Issuer").Value!;
        var audience = configuration.GetSection("JWT:Audience").Value!;

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    RoleClaimType = ClaimTypes.Role
                };
            });

        services.AddAuthorization();

        return services;
    }

    internal static IServiceCollection AddMongoDb(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IMongoClient>(_ =>
            new MongoClient(configuration.GetSection("MongoDb:Address").Value!));

        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(configuration.GetSection("MongoDb:DatabaseName").Value!);
        });

        return services;
    }
}