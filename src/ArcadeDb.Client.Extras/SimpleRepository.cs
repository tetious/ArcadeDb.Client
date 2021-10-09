using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace ArcadeDb.Client.Extras;

public interface ISimpleRepository<T>
    where T : Entity
{
    Task<T?> Get(string recordId);

    Task<T?> Get(Guid id);

    Task<T[]> Query(string whereFragment, object parameters);

    Task<T> Insert(T entity);

    Task<T> Update(T entity);

    Task Delete(Guid id);
}

public class SimpleRepository<T> : ISimpleRepository<T>
    where T : Entity
{
    private readonly ArcadeDatabase database;
    private readonly IClock clock;
    private readonly string createTemplate;
    private readonly string updateTemplate;
    private static readonly string EntityName = typeof(T).Name;

    public SimpleRepository(ArcadeDatabase database, IClock clock)
    {
        this.database = database;
        this.clock = clock;

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(pi => pi.IsInitOnly() && pi.Name != nameof(Entity.Rid)).ToArray();

        string paramList(IEnumerable<PropertyInfo> props) => string.Join(",", props.Select(pi => $"\"{pi.Name.ToCamelCase()}\": :{pi.Name.ToCamelCase()}"));

        // TODO: Handle sub-documents as embedded references? : { "@type": "d",
        this.createTemplate = $"{{ {paramList(properties)} }}";
        this.updateTemplate = $"{{ {paramList(properties.Where(pi => pi.Name != nameof(Entity.Id)))} }}";

        // TODO: This should happen somewhere else and support both DOCUMENT and VERTEX
        this.database.Execute<JsonElement>($"CREATE DOCUMENT TYPE {EntityName} IF NOT EXISTS").Wait();
    }

    public async Task<T?> Get(string recordId)
    {
        if (ProtocolHelpers.IsRecordId(recordId) == false) throw new ArgumentException("RecordId must be in the form #0.0.");
        var result = await this.database.Query<T>("SELECT FROM :recordId", new { recordId });
        return result.SingleOrDefault();
    }

    public async Task<T?> Get(Guid id)
    {
        var result = await this.database.Query<T>($"SELECT FROM {EntityName} WHERE id=:id", new { id });
        return result.SingleOrDefault();
    }

    public async Task<T[]> Query(string whereFragment, object parameters)
    {
        return await this.database.Query<T>($"SELECT FROM {EntityName} WHERE {whereFragment}", parameters);
    }

    public async Task<T> Insert(T entity)
    {
        entity = entity with { Id = Guid.NewGuid(), CreatedDate = this.clock.GetCurrentInstant() };
        var result = await this.database.Execute<JsonElement>($"INSERT INTO {EntityName} CONTENT {this.createTemplate} RETURN @rid", entity);
        entity = entity with { Rid = result.SingleOrDefault().GetProperty("@rid").GetString() ?? throw new ArcadeDbException("Create failed.") };
        return entity;
    }

    public async Task<T> Update(T entity)
    {
        entity = entity with { UpdatedDate = this.clock.GetCurrentInstant() };
        await this.database.Execute<JsonElement>($"UPDATE {EntityName} CONTENT {this.updateTemplate} WHERE id=:id", entity);
        return entity;
    }

    public async Task Delete(Guid id)
    {
        await this.database.Execute<JsonElement>($"DELETE FROM {EntityName} WHERE id=:id", new { id });
    }
}
