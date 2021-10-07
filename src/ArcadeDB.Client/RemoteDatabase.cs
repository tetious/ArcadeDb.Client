using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static ArcadeDb.Client.QueryLanguage;

namespace ArcadeDb.Client;

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

    public async Task Create(string? database = null) =>
        await this.HttpExecute("create", database ?? this.Database);

    public async Task Drop(string? database = null) =>
        await this.HttpExecute("drop", database ?? this.Database);

    public async Task<T[]> Query<T>(string command, params object[] parameters)
        where T : class => await this.Query<T>(command, Sql, parameters).ConfigureAwait(false);

    public async Task<T[]> Query<T>(string command, IEnumerable<object>? parameters = null)
        where T : class => await this.Query<T>(command, Sql, parameters).ConfigureAwait(false);

    public async Task<T[]> Query<T>(string command, QueryLanguage language, params object[] parameters)
        where T : class => await this.Query<T>(command, language, (IEnumerable<object>)parameters).ConfigureAwait(false);

    public async Task<T[]> Query<T>(string command, QueryLanguage language, IEnumerable<object>? parameters = null)
        where T : class => await this.HttpExecute<T>("query", this.Database, command, parameters?.ToArray(), language).ConfigureAwait(false);

    public async Task<T[]> Execute<T>(string command, params object[] parameters)
        where T : class => await this.Execute<T>(command, Sql, parameters).ConfigureAwait(false);

    public async Task<T[]> Execute<T>(string command, IEnumerable<object>? parameters = null)
        where T : class => await this.Execute<T>(command, Sql, parameters).ConfigureAwait(false);

    public async Task<T[]> Execute<T>(string command, QueryLanguage language, params object[] parameters)
        where T : class => await this.Execute<T>(command, language, (IEnumerable<object>)parameters).ConfigureAwait(false);

    public async Task<T[]> Execute<T>(string command, QueryLanguage language, IEnumerable<object>? parameters = null)
        where T : class => await this.HttpExecute<T>("command", this.Database, command, parameters?.ToArray(), language).ConfigureAwait(false);

    private async Task<T[]> HttpExecute<T>(string operation, string database, string? command = null, object[]? parameters = null, QueryLanguage language = Sql)
        where T : class
    {
        if (parameters == null || parameters.Length == 1)
        {
            return await this.HttpExecuteSingle<T>(operation, database, command, parameters?.Single(), language);
        }

        var list = new List<T>();
        foreach (var parameter in parameters)
        {
            list.AddRange(await this.HttpExecuteSingle<T>(operation, database, command, parameter, language));
        }

        return list.ToArray();
    }

    private async Task<T[]> HttpExecuteSingle<T>(string operation, string database, string? command = null, object? parameters = null,
        QueryLanguage language = Sql)
        where T : class
    {
        using var response = await this.HttpClientExecute(operation, database, command, parameters, language);

        var databaseResult = await JsonSerializer.DeserializeAsync<DatabaseResult<T>>(await response.Content.ReadAsStreamAsync(), Json.DefaultSerializerOptions)
            .ConfigureAwait(false) ?? throw new ArcadeDbException($"Could not deserialize result as {nameof(T)}.");

        return databaseResult.Result;
    }

    private async Task<string> HttpExecute(string operation, string database, string? command = null, object? parameters = null, QueryLanguage language = Sql)
    {
        using var response = await this.HttpClientExecute(operation, database, command, parameters, language);
        return (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync()).ConfigureAwait(false))
            .RootElement.GetProperty("result").GetString() ?? throw new ArcadeDbException("Could not deserialize to string.");
    }

    private async Task<HttpResponseMessage> HttpClientExecute(string operation, string database, string? command = null, object? parameters = null,
        QueryLanguage language = Sql)
    {
        var requestJson = command == null
            ? "{}"
            : new { language = language.ToString().ToLower(), command, serializer = "record", @params = parameters }.ToJson();
        using var requestBody = new StringContent(requestJson, Encoding.UTF8, "application/json");

        var response = await this.httpClient.PostAsync($"{operation}/{database}", requestBody);
        if (response.IsSuccessStatusCode == false)
        {
            if (response.StatusCode != HttpStatusCode.InternalServerError) response.EnsureSuccessStatusCode();
            var error = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            if (!error.RootElement.TryGetProperty("exception", out var exception))
            {
                throw new ArcadeDbException($"An unknown error has occured.\n{error.RootElement.GetString()}");
            }

            var exceptionName = exception.GetString()?.Split(".").LastOrDefault();
            throw exceptionName switch
            {
                nameof(ParseException) or "SyntaxException" => new ParseException(error.RootElement.GetProperty("detail").GetString()),
                null => new ArcadeDbException($"An unknown error has occured.\n{error.RootElement.GetString()}"),
                _ => new ArcadeDbException(error.RootElement.GetProperty("detail").GetString())
            };
        }

        Debug.WriteLine(await response.Content.ReadAsStringAsync());

        return response;
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
