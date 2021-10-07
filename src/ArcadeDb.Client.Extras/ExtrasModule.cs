global using System;
global using System.Linq;
global using NodaTime;

using NodaTime.Serialization.SystemTextJson;

namespace ArcadeDb.Client.Extras;

public class ExtrasModule
{
    public static void Initialize()
    {
        Json.DefaultSerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }
}
