namespace DivarExtensionDemo.Services.Interfaces;

public interface IDivarService
{
    Task<string> GenerateSsoAuthUrlAsync(string postToken, CancellationToken cancellationToken);

    Task<string> HandleFallbackAsync(string state, string code, CancellationToken cancellationToken);
}