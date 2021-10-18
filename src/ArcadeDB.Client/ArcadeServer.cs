using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Ardalis.GuardClauses;
using static System.Net.Http.HttpMethod;

namespace ArcadeDb.Client;

public class ArcadeServer : IDisposable
{
    private const string SessionIdHeaderName = "arcadedb-session-id";
    private readonly HttpClient httpClient;

    /// <summary>
    /// Configure a connection to an ArcadeDb server.
    /// </summary>
    /// <param name="connectionString">Example: http[s]://username:password@localhost:2480</param>
    public ArcadeServer(string connectionString)
    {
        var parsed = new Uri(connectionString);
        var userPassword = parsed.UserInfo.Split(":");
        var (url, username, password) = ($"{parsed.Scheme}://{parsed.Host}:{parsed.Port}", userPassword[0], userPassword[1]);

        this.httpClient = new HttpClient
        {
            BaseAddress = new Uri($"{url}/api/v1/"),
            // TODO: Cleanup
            DefaultRequestHeaders =
            {
                Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"))),
                Accept = { new MediaTypeWithQualityHeaderValue("application/json") }
            }
        };
    }

    /// <summary>
    /// List the databases on the server.
    /// </summary>
    /// <returns>A list of databases.</returns>
    public async Task<IReadOnlyList<string>> ListDatabases() => await this.HttpExecute(Get, "databases");

    /// <summary>
    /// Use the given database.
    /// </summary>
    /// <param name="database">The database name.</param>
    /// <returns>An ArcadeDatabase attached to the specified database.</returns>
    public ArcadeDatabase Use(string database) => this.GetDatabase(database);

    /// <summary>
    /// Get the given database. (Synonym for Use.)
    /// </summary>
    /// <param name="database">The database name.</param>
    /// <returns>An ArcadeDatabase attached to the specified database.</returns>
    public ArcadeDatabase GetDatabase(string database) => new ArcadeDatabase(this, database);

    /// <summary>
    /// Create the specified database.
    /// </summary>
    /// <param name="database"></param>
    public async Task Create(string database) => await this.HttpExecuteScalar(Post, "create", database);

    /// <summary>
    /// Drop the specified database.
    /// </summary>
    /// <param name="database"></param>
    public async Task Drop(string database) => await this.HttpExecuteScalar(Post, "drop", database);

    /// <summary>
    /// Begin a server-side transaction on the specified database.
    /// </summary>
    /// <param name="database"></param>
    /// <returns>The transaction's corresponding session-id.</returns>
    public async Task<string> Begin(string database)
    {
        Guard.Against.NullOrWhiteSpace(database, nameof(database));

        var response = await this.HttpClientExecute(Post, "begin", database, null, null);
        return response.Headers.GetValues(SessionIdHeaderName).Single();
    }

    /// <summary>
    /// Commit the specified server-side transaction on the specified database.
    /// </summary>
    /// <param name="database"></param>
    /// <param name="sessionId">The session-id from Begin.</param>
    public async Task Commit(string database, string sessionId)
    {
        Guard.Against.NullOrWhiteSpace(database, nameof(database));
        Guard.Against.NullOrWhiteSpace(sessionId, nameof(sessionId));

        await this.HttpClientExecute(Post, "commit", database, null, sessionId);
    }

    /// <summary>
    /// Rollback the specified server-side transaction on the specified database.
    /// </summary>
    /// <param name="database"></param>
    /// <param name="sessionId">The session-id from Begin.</param>
    public async Task Rollback(string database, string sessionId)
    {
        Guard.Against.NullOrWhiteSpace(database, nameof(database));
        Guard.Against.NullOrWhiteSpace(sessionId, nameof(sessionId));

        await this.HttpClientExecute(Post, "rollback", database, null, sessionId);
    }

    /// <summary>
    /// Execute an arbitrary command on the server.
    /// </summary>
    /// <param name="operation">The operation to execute. See the HTTP API docs (https://docs.arcadedb.com/#HTTP-API) for more details.</param>
    /// <param name="database"></param>
    /// <param name="command">The command query to execute. Only valid for command and query operations.</param>
    /// <param name="parameters">An object with properties and values that correspond to the parameterized command. Supports an array of objects.</param>
    /// <param name="language">The query language.</param>
    /// <param name="sessionId">The session-id that corresponds to the server-side transaction you wish to enlist in.</param>
    /// <typeparam name="T">The type the results should be serialized into. Use JsonElement or dynamic for maximum flexibility.</typeparam>
    /// <returns>Array of T.</returns>
    public async Task<T[]> HttpExecute<T>(string operation, string database, string? command = null, object[]? parameters = null,
        QueryLanguage language = QueryLanguage.Sql, string? sessionId = null)
    {
        Guard.Against.NullOrWhiteSpace(operation, nameof(operation));
        Guard.Against.NullOrWhiteSpace(database, nameof(database));

        if (parameters == null || parameters.Length < 2)
        {
            return await this.HttpExecuteSingle<T>(operation, database, new CommandPayload(command, parameters?.SingleOrDefault(), language), sessionId);
        }

        var list = new List<T>();
        foreach (var parameter in parameters)
        {
            list.AddRange(await this.HttpExecuteSingle<T>(operation, database, new CommandPayload(command, parameter, language), sessionId));
        }

        return list.ToArray();
    }

    private async Task<string> HttpExecuteScalar(HttpMethod method, string operation, string? database = null)
    {
        using var response = await this.HttpClientExecute(method, operation, database, null, null);
        return (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync()).ConfigureAwait(false))
            .RootElement.GetProperty("result").GetString() ?? throw new ArcadeDbException("Could not deserialize to string.");
    }

    private async Task<string[]> HttpExecute(HttpMethod method, string operation, string? database = null)
    {
        using var response = await this.HttpClientExecute(method, operation, database, null, null);
        var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync()).ConfigureAwait(false);

        return (document.RootElement.GetProperty("result").EnumerateArray().Select(e => e.GetString()).ToArray()
            ?? throw new ArcadeDbException("Could not deserialize to string."))!;
    }

    private async Task<T[]> HttpExecuteSingle<T>(string operation, string? database, CommandPayload? payload, string? sessionId)
    {
        using var response = await this.HttpClientExecute(Post, operation, database, payload, sessionId);

        var databaseResult = await JsonSerializer.DeserializeAsync<DatabaseResult<T>>(await response.Content.ReadAsStreamAsync(), Json.DefaultSerializerOptions)
            .ConfigureAwait(false) ?? throw new ArcadeDbException($"Could not deserialize result as {nameof(T)}.");

        return databaseResult.Result;
    }

    private async Task<HttpResponseMessage> HttpClientExecute(HttpMethod method, string operation, string? database, CommandPayload? payload, string? sessionId)
    {
        var request = new HttpRequestMessage(method, $"{operation}{(database == null ? string.Empty : $"/{database}")}")
        {
            Content = payload?.ToStringContent()
        };

        if (string.IsNullOrWhiteSpace(sessionId) == false)
        {
            request.Headers.Add(SessionIdHeaderName, sessionId);
        }

        var response = await this.httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode == false)
        {
            if (response.StatusCode != HttpStatusCode.InternalServerError) response.EnsureSuccessStatusCode();
            var error = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            if (error.RootElement.TryGetProperty("exception", out var exception) == false)
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

    public void Dispose()
    {
        this.httpClient.Dispose();
    }
}
