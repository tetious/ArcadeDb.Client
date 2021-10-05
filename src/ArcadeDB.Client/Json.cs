using System.Text.Json;
using System.Text.Json.Serialization;

public static class Json
{
    public static readonly JsonSerializerOptions SerializerOptions = SetSerializerSettings(new JsonSerializerOptions());

    public static JsonSerializerOptions SetSerializerSettings(JsonSerializerOptions settings)
    {
        settings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        settings.Converters.Add(new JsonStringEnumConverter());
        settings.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        //settings.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        return settings;
    }

    public static string ToJson(this object obj) => JsonSerializer.Serialize(obj, SerializerOptions);

    public static T? ToObject<T>(this string str) => JsonSerializer.Deserialize<T>(str, SerializerOptions);

    public static T? ToObject<T>(this JsonElement element) => JsonSerializer.Deserialize<T>(element.GetRawText(), SerializerOptions);

    public static T? ToObject<T>(this JsonDocument document) => JsonSerializer.Deserialize<T>(document.RootElement.GetRawText(), SerializerOptions);
}
