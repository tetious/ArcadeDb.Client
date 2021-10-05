using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ArcadeDb.Client;

public enum QueryLanguage
{
    Sql,
    Cypher,
    Gremlin,
    MongoDb
}

// {"exception":"java.lang.IllegalArgumentException","detail":"Database 'banana-pie' already exists","error":"Internal error"}
// { "result" : "ok"}

public record DatabaseResult(string? Exception, string? Detail, string? Error, string? Result)
{
    public bool IsError => this.Error != null;

    public static DatabaseResult UnknownError() => new DatabaseResult(null, null, "An unknown error has occurred.", null);
}

public class RemoteDatabase : IDisposable
{
    private readonly HttpClient httpClient;

    public string Database { get; private set; }

    // TODO: ConnectionString
    public RemoteDatabase(string uri, string database, string username, string password)
    {
        this.Database = database;
        this.httpClient = new HttpClient
        {
            BaseAddress = new Uri($"{uri}/api/v1/"),
            // TODO: Cleanup
            DefaultRequestHeaders =
            {
                Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"))),
                Accept = { new MediaTypeWithQualityHeaderValue("application/json") }
            }
        };
    }

    public void Use(string database) => this.Database = database;

    public async Task<DatabaseResult> Create(string? database = null) =>
        (await this.HttpCommand("create", database ?? this.Database)).ToObject<DatabaseResult>() ?? DatabaseResult.UnknownError();

    public async Task<DatabaseResult> Drop(string? database = null) =>
        (await this.HttpCommand("drop", database ?? this.Database)).ToObject<DatabaseResult>() ?? DatabaseResult.UnknownError();

    public async Task<JsonElement> Command(string command, object @params, QueryLanguage language = QueryLanguage.Sql) =>
        await this.HttpCommand("command", this.Database, command, @params, language);

    public async Task<JsonElement> Query(string command, object @params, QueryLanguage language = QueryLanguage.Sql) =>
        await this.HttpCommand("query", this.Database, command, @params, language);

    private async Task<JsonElement> HttpCommand(string operation, string database, string? command = null, object? @params = null,
        QueryLanguage language = QueryLanguage.Sql)
    {
        var requestJson = command == null ? "{}" : new { language = language.ToString().ToLower(), command, serializer = "record", @params }.ToJson();
        using var requestBody = new StringContent(requestJson, Encoding.UTF8, "application/json");

        using var response = await this.httpClient.PostAsync($"{operation}/{database}", requestBody);
        if (response.IsSuccessStatusCode == false)
        {
            if (response.StatusCode != HttpStatusCode.InternalServerError) response.EnsureSuccessStatusCode();
            var error = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            if (!error.RootElement.TryGetProperty("exception", out var exception)) return error.RootElement;
            var exceptionName = exception.GetString()?.Split(".").LastOrDefault();

            throw exceptionName switch
            {
                nameof(ParseException) or "SyntaxException" => new ParseException(error.RootElement.GetProperty("detail").GetString()),
                null => new ArcadeDbException($"An unknown error has occured.\n{error.RootElement.GetString()}"),
                _ => new ArcadeDbException(error.RootElement.GetProperty("detail").GetString())
            };
        }

        var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return document.RootElement;
    }

    private void RequestClusterConfiguration()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        this.httpClient.Dispose();
    }
}
