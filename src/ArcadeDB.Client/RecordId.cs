using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ArcadeDb.Client;

[JsonConverter(typeof(RecordIdConverter))]
public readonly struct RecordId
{
    private readonly string recordId;

    private static readonly Regex RidRegex = new(@"^#\d+:\d+$", RegexOptions.Compiled);

    public RecordId(string? recordId)
    {
        if (IsRecordId(recordId) == false) throw new ArgumentException($"{recordId} is not a valid Record Id", nameof(recordId));
        this.recordId = recordId!;
    }

    public static implicit operator RecordId(string recordId) => new RecordId(recordId);

    public static implicit operator string(RecordId recordId) => recordId.recordId;

    public override string ToString() => this.recordId;

    public static bool IsRecordId(string? candidate) => RidRegex.IsMatch(candidate ?? string.Empty);
}

internal class RecordIdConverter : JsonConverter<RecordId>
{
    public override RecordId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(reader.GetString());

    public override void Write(Utf8JsonWriter writer, RecordId value, JsonSerializerOptions options) => writer.WriteStringValue(value);
}
