using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using ArcadeDb.Client.Extras;

namespace ArcadeDb.Client;

public abstract record Entity
{
    public string? Rid { get; init; }

    public Instant? CreatedDate { get; init; }

    public Instant? UpdatedDate { get; init; }
}

public class SimpleRepository<T> : IDisposable
    where T : Entity
{
    private readonly RemoteDatabase database;
    private readonly IClock clock;
    private readonly string propertiesTemplate;
    private static readonly string EntityName = typeof(T).Name;
    private static readonly Regex RidRegex = new Regex(@"^#\d+:\d+$", RegexOptions.Compiled);

    public SimpleRepository(RemoteDatabase database, IClock clock)
    {
        this.database = database;
        this.clock = clock;

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(pi => pi.IsInitOnly() && pi.Name != "Id");
        this.propertiesTemplate = $"{{ {string.Join(",", properties.Select(pi => $"\"{pi.Name.ToCamelCase()}\": :{pi.Name.ToCamelCase()}"))} }}";

        // TODO: This should happen somewhere else!
        this.database.Execute<JsonElement>($"CREATE DOCUMENT TYPE {EntityName} IF NOT EXISTS").Wait();
    }

    public async Task<T> Get(string recordId)
    {
        if (RidRegex.IsMatch(recordId) == false) throw new ArgumentException("Id must be a Record Id in the form of #0.0.");
        var result = await this.database.Query<T>($"SELECT FROM {EntityName} WHERE @rid={recordId}");
        return result.Single();
    }

    public async Task<T> Insert(T entity)
    {
        entity = entity with { CreatedDate = this.clock.GetCurrentInstant() };
        var templateString = $"INSERT INTO {EntityName} CONTENT {this.propertiesTemplate} RETURN @rid";
        var result = await this.database.Execute<JsonElement>(templateString, entity);
        entity = entity with { Rid = result.Single().GetProperty("@rid").GetString() };
        return entity;
    }

    public void Dispose()
    {
        this.database.Dispose();
    }
}
