using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using static System.Net.Http.HttpMethod;

namespace ArcadeDb.Client;

public class ArcadeServer : IDisposable
{
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

    public async Task<IReadOnlyList<string>> ListDatabases() => await this.HttpExecute(Get, "databases");

    public ArcadeDatabase Use(string database) => this.GetDatabase(database);

    public ArcadeDatabase GetDatabase(string database) => new ArcadeDatabase(this, database);

    public async Task Create(string database) => await this.HttpExecuteScalar(Post, "create", database);

    public async Task Drop(string database) => await this.HttpExecuteScalar(Post, "drop", database);

    public async Task<T[]> HttpExecute<T>(string operation, string database, string? command = null, object[]? parameters = null,
        QueryLanguage language = QueryLanguage.Sql)
    {
        if (parameters == null || parameters.Length == 1)
        {
            return await this.HttpExecuteSingle<T>(operation, database, new CommandPayload(command, parameters?.Single(), language));
        }

        var list = new List<T>();
        foreach (var parameter in parameters)
        {
            list.AddRange(await this.HttpExecuteSingle<T>(operation, database, new CommandPayload(command, parameter, language)));
        }

        return list.ToArray();
    }

    private async Task<string> HttpExecuteScalar(HttpMethod method, string operation, string? database = null)
    {
        using var response = await this.HttpClientExecute(method, operation, database);
        return (await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync()).ConfigureAwait(false))
            .RootElement.GetProperty("result").GetString() ?? throw new ArcadeDbException("Could not deserialize to string.");
    }

    private async Task<string[]> HttpExecute(HttpMethod method, string operation, string? database = null)
    {
        using var response = await this.HttpClientExecute(method, operation, database);
        var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync()).ConfigureAwait(false);

        return document.RootElement.GetProperty("result").EnumerateArray().Select(e => e.GetString()).ToArray()
            ?? throw new ArcadeDbException("Could not deserialize to string.");
    }

    private async Task<T[]> HttpExecuteSingle<T>(string operation, string? database, CommandPayload? payload)
    {
        using var response = await this.HttpClientExecute(Post, operation, database, payload);

        var databaseResult = await JsonSerializer.DeserializeAsync<DatabaseResult<T>>(await response.Content.ReadAsStreamAsync(), Json.DefaultSerializerOptions)
            .ConfigureAwait(false) ?? throw new ArcadeDbException($"Could not deserialize result as {nameof(T)}.");

        return databaseResult.Result;
    }

    private async Task<HttpResponseMessage> HttpClientExecute(HttpMethod method, string operation, string? database = null, CommandPayload? payload = null)
    {
        var request = new HttpRequestMessage(method, $"{operation}{(database == null ? string.Empty : $"/{database}")}")
        {
            Content = payload?.ToStringContent()
        };

        var response = await this.httpClient.SendAsync(request);
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

    public void Dispose()
    {
        this.httpClient.Dispose();
    }
}
