namespace DivarExtensionDemo.Models;

public sealed class AuthAccessTokenResponse
{
    public string Access_Token { get; set; } = null!;
    public int Expires_In { get; set; }
    public string Scope { get; set; }
    public string Token_Type { get; set; } = null!;
}