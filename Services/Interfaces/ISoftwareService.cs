namespace DivarExtensionDemo.Services.Interfaces;

public interface ISoftwareService
{
    Dictionary<string, string> GetNamesAsync(CancellationToken cancellationToken);
}