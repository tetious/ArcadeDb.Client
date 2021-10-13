global using System;
global using System.Linq;
global using NodaTime;
using Microsoft.Extensions.DependencyInjection;
using NodaTime.Serialization.SystemTextJson;

namespace ArcadeDb.Client.Extras;

public static class ExtrasModule
{
    public static void Initialize()
    {
        Json.DefaultSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }

    public static IServiceCollection AddArcadeDbClientExtras(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddSingleton(typeof(ISimpleRepository<>), typeof(SimpleRepository<>))
            .AddSingleton(typeof(IVertexRepository<>), typeof(VertexRepository<>));
    }
}
