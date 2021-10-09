namespace ArcadeDb.Client.Extras;

public abstract record Entity
{
    public Guid Id { get; init; }

    public string Rid { get; init; }

    public Instant CreatedDate { get; init; }

    public Instant? UpdatedDate { get; init; }
}
