using System.Text.Json.Serialization;

namespace ArcadeDb.Client.Extras;

public abstract record Entity
{
    [JsonPropertyName("@rid")]
    public RecordId RecordId { get; init; }

    public Instant CreatedDate { get; init; }

    public Instant? UpdatedDate { get; init; }

    private Entity() { }

    public abstract record Vertex : Entity { }

    public abstract record Document : Entity { }
}

public abstract record Edge
{
    [JsonPropertyName("edgeRid")]
    public RecordId RecordId { get; init; }

    [JsonPropertyName("@in")]
    public RecordId InRecordId { get; init; }

    [JsonPropertyName("@out")]
    public RecordId OutRecordId { get; init; }
}
