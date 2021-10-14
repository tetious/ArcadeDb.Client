using System.Reflection;
using System.Text.Json;

namespace ArcadeDb.Client.Extras;

public interface ISimpleRepository<T>
    where T : Entity
{
    Task<T?> Get(RecordId recordId);

    Task<T?> Get(Guid id);

    Task<T[]> Query(string whereFragment, object? parameters = null);

    Task<T[]> QueryCypher(string query, object? parameters = null);

    Task<T> Insert(T entity);

    Task<T> Update(T entity);

    Task Delete(Guid id);
}

public class SimpleRepository<T> : ISimpleRepository<T>
    where T : Entity
{
    protected readonly ArcadeDatabase Database;
    protected readonly IClock Clock;
    private readonly string createTemplate;
    private readonly string updateTemplate;

    protected static readonly string EntityName = typeof(T).Name;

    public SimpleRepository(ArcadeDatabase database, IClock clock)
    {
        this.Database = database;
        this.Clock = clock;

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(pi => pi.CanWrite && pi.Name != nameof(Entity.RecordId)).ToArray();

        string paramList(IEnumerable<PropertyInfo> props) => string.Join(",", props.Select(pi => $"\"{pi.Name.ToCamelCase()}\": :{pi.Name.ToCamelCase()}"));

        // TODO: Handle sub-documents as embedded references? : { "@type": "d",
        this.createTemplate = $"{{ {paramList(properties)} }}";
        this.updateTemplate = $"{{ {paramList(properties)} }}";

        // TODO: This should happen somewhere else.
        var entityType = typeof(T).IsAssignableTo(typeof(Entity.Vertex)) ? "VERTEX" : "DOCUMENT";
        this.Database.Execute<JsonElement>($"CREATE {entityType} TYPE {EntityName} IF NOT EXISTS").Wait();
    }

    public async Task<T?> Get(RecordId recordId)
    {
        var result = await this.Database.Query<T>("SELECT FROM :recordId", new { recordId });
        return result.SingleOrDefault();
    }

    public async Task<T?> Get(Guid id)
    {
        var result = await this.Database.Query<T>($"SELECT FROM {EntityName} WHERE id=:id", new { id });
        return result.SingleOrDefault();
    }

    public async Task<T[]> Query(string whereFragment, object? parameters = null)
    {
        return await this.Database.Query<T>($"SELECT FROM {EntityName} WHERE {whereFragment}", parameters);
    }

    public async Task<T[]> QueryCypher(string query, object? parameters = null)
    {
        return await this.Database.Query<T>(query, QueryLanguage.Cypher, parameters);
    }

    public async Task<T> Insert(T entity)
    {
        entity = entity with { Id = Guid.NewGuid(), CreatedDate = this.Clock.GetCurrentInstant() };
        var result = await this.Database.Execute<JsonElement>($"INSERT INTO {EntityName} CONTENT {this.createTemplate} RETURN @rid", entity);
        entity = entity with { RecordId = result.SingleOrDefault().GetProperty("@rid").GetString() ?? throw new ArcadeDbException("Create failed.") };
        return entity;
    }

    public async Task<T> Update(T entity)
    {
        entity = entity with { UpdatedDate = this.Clock.GetCurrentInstant() };
        await this.Database.Execute<JsonElement>($"UPDATE {EntityName} CONTENT {this.updateTemplate} WHERE id=:id", entity);
        return entity;
    }

    public async Task Delete(Guid id)
    {
        await this.Database.Execute<JsonElement>($"DELETE FROM {EntityName} WHERE id=:id", new { id });
    }
}
