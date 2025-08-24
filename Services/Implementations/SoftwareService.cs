using DivarExtensionDemo.Entities;
using DivarExtensionDemo.Services.Interfaces;
using MongoDB.Driver;

namespace DivarExtensionDemo.Services.Implementations;

public sealed class SoftwareService(IMongoDatabase mongoDatabase) : ISoftwareService
{
    public Dictionary<string, string> GetNamesAsync(CancellationToken cancellationToken)
    {
        var softwares = mongoDatabase.GetCollection<Software>("Softwares")
            .AsQueryable()
            .Select(s => new { s.Id, s.Name })
            .ToDictionary(s => s.Id, s => s.Name);

        return softwares;
    }
}