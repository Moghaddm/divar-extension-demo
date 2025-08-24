using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DivarExtensionDemo.Infrastructure.JWT;

internal static class JwtFactory
{
    internal static string CreateAccessToken(IConfiguration configuration, List<Claim> customClaims, string role)
    {
        List<Claim> claims = [];
        claims.AddRange(customClaims);
        claims.Add(new Claim(ClaimTypes.Role, role));

        var jwtKey = configuration.GetSection("JWT:Key").Value!;
        var issuer = configuration.GetSection("JWT:Issuer").Value!;
        var audience = configuration.GetSection("JWT:Audience").Value!;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(60),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}