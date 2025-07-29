namespace DivarExtensionDemo.Infrastructure.Divar.Models;

public sealed class AccessTokenResponse
{
    public string AccessToken { get; init; } = null!;
    public int ExpiresIn { get; init; }
    public string Scope { get; init; } = null!;
    public string TokenType { get; init; } = null!;
}