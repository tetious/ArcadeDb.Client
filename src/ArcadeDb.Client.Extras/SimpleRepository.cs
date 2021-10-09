using System.Reflection;
using System.Text.Json;

namespace ArcadeDb.Client.Extras;

public abstract record Entity
{
    public Guid Id { get; init; }

    public string Rid { get; init; }

    public Instant CreatedDate { get; init; }

    public Instant? UpdatedDate { get; init; }
}

public class SimpleRepository<T>
    where T : Entity
{
    private readonly ArcadeDatabase database;
    private readonly IClock clock;
    private readonly string propertiesTemplate;
    private static readonly string EntityName = typeof(T).Name;

    public SimpleRepository(ArcadeDatabase database, IClock clock)
    {
        this.database = database;
        this.clock = clock;

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(pi => pi.IsInitOnly() && pi.Name != nameof(Entity.Rid));
        this.propertiesTemplate = $"{{ {string.Join(",", properties.Select(pi => $"\"{pi.Name.ToCamelCase()}\": :{pi.Name.ToCamelCase()}"))} }}";

        // TODO: This should happen somewhere else!
        this.database.Execute<JsonElement>($"CREATE DOCUMENT TYPE {EntityName} IF NOT EXISTS").Wait();
    }

    public async Task<T?> Get(string recordId)
    {
        if (ProtocolHelpers.IsRecordId(recordId) == false) throw new ArgumentException("RecordId must be in the form of #0.0.");
        var result = await this.database.Query<T>("SELECT FROM :recordId", new { recordId });
        return result.SingleOrDefault();
    }

    public async Task<T?> Get(Guid id)
    {
        var result = await this.database.Query<T>($"SELECT FROM {EntityName} WHERE id=:id", new { id });
        return result.SingleOrDefault();
    }

    public async Task<T> Insert(T entity)
    {
        entity = entity with { Id = Guid.NewGuid(), CreatedDate = this.clock.GetCurrentInstant() };
        var result = await this.database.Execute<JsonElement>($"INSERT INTO {EntityName} CONTENT {this.propertiesTemplate} RETURN @rid", entity);
        entity = entity with { Rid = result.SingleOrDefault().GetProperty("@rid").GetString() ?? throw new ArcadeDbException("Create failed.") };
        return entity;
    }
}
