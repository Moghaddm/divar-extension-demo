using DivarExtensionDemo.Entities;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace DivarExtensionDemo.Infrastructure.MongoDb;

internal static class DatabaseSeeder
{
    internal static async Task DataSeedAsync(this IApplicationBuilder applicationBuilder)
    {
        var softwareCollection = applicationBuilder.ApplicationServices.GetRequiredService<IMongoDatabase>()
            .GetCollection<Software>("Softwares");

        if (await softwareCollection.AsQueryable().CountAsync(CancellationToken.None) is 0)
        {
            List<Software> softwares =
            [
                new() { Id = Guid.CreateVersion7().ToString(), Name = "After Effects 2024" },
                new() { Id = Guid.CreateVersion7().ToString(), Name = "Adobe Premiere 2024" },
                new() { Id = Guid.CreateVersion7().ToString(), Name = "Red Dead Redemption 2" }
            ];
            await softwareCollection.InsertManyAsync(softwares, cancellationToken: CancellationToken.None);
        }
    }
}