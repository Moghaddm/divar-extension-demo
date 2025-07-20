namespace DivarExtensionDemo.Models.Divar;

public sealed class AccessTokenResponse
{
    public string Access_Token { get; init; } = null!;
    public int Expires_In { get; init; }
    public string Scope { get; init; }
    public string Token_Type { get; init; } = null!;
}