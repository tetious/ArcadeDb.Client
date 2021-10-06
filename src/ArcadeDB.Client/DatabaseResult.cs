using System.Text.Json;

namespace ArcadeDb.Client;

public record DatabaseResult(string? Exception, string? Detail, string? Error)
{
    public bool IsError => this.Error != null;

    public JsonElement Result { get; init; }

    public static DatabaseResult UnknownError() => new DatabaseResult(null, null, "An unknown error has occurred.");
}

public record DatabaseResult<T>(string? Exception, string? Detail, string? Error) : DatabaseResult(Exception, Detail, Error)
{
    public new T Result { get; init; }
}
