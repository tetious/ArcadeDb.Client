using System.Text.Json;
using Ardalis.GuardClauses;

namespace ArcadeDb.Client;

public abstract class ArcadeDatabaseHandle
{
    protected readonly string? SessionId;
    protected readonly ArcadeServer Server;

    public string Database { get; }

    protected internal ArcadeDatabaseHandle(ArcadeServer server, string database, string? sessionId = null)
    {
        this.SessionId = sessionId;
        this.Server = Guard.Against.Null(server, nameof(server));
        this.Database = Guard.Against.NullOrEmpty(database, nameof(database));
    }

    public async Task<T[]> Query<T>(string command, params object[] parameters)
        => await this.Query<T>(command, QueryLanguage.Sql, parameters).ConfigureAwait(false);

    public async Task<T[]> Query<T>(string command, QueryLanguage language, params object[] parameters)
        => await this.Query<T>(command, language, (IEnumerable<object>)parameters).ConfigureAwait(false);

    public async Task<T[]> Query<T>(string command, QueryLanguage language, IEnumerable<object>? parameters = null)
        => await this.Server.HttpExecute<T>("query", this.Database, command, parameters?.ToArray(), language, this.SessionId).ConfigureAwait(false);

    public async Task Execute(string command, params object[] parameters)
        => await this.Execute<JsonElement>(command, QueryLanguage.Sql, parameters).ConfigureAwait(false);

    public async Task<T[]> Execute<T>(string command, params object[] parameters)
        => await this.Execute<T>(command, QueryLanguage.Sql, parameters).ConfigureAwait(false);

    public async Task<T[]> Execute<T>(string command, QueryLanguage language, params object[] parameters)
        => await this.Execute<T>(command, language, (IEnumerable<object>)parameters).ConfigureAwait(false);

    public async Task Execute(string command, QueryLanguage language, params object[] parameters)
        => await this.Execute<JsonElement>(command, language, parameters).ConfigureAwait(false);

    public async Task<T[]> Execute<T>(string command, QueryLanguage language, IEnumerable<object>? parameters = null)
        => await this.Server.HttpExecute<T>("command", this.Database, command, parameters?.ToArray(), language, this.SessionId).ConfigureAwait(false);
}
