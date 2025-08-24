using DivarExtensionDemo.Models.Comparision;

namespace DivarExtensionDemo.Services.Interfaces;

public interface IComparisionService
{
    Task<string> CreateAsync(
        string postToken,
        string[] softwareIds,
        string divarAccessToken,
        CancellationToken cancellationToken
    );

    Task<ComparisionDto> GetAsync(string id, CancellationToken cancellationToken);
}