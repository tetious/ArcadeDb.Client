using static ArcadeDb.Client.QueryLanguage;

namespace ArcadeDb.Client;

public class ArcadeDatabase
{
    private readonly ArcadeServer server;

    public string Database { get; }

    public ArcadeDatabase(ArcadeServer server, string database)
    {
        this.server = server;
        this.Database = database;
    }

    public async Task<T[]> Query<T>(string command, params object[] parameters)
        => await this.Query<T>(command, Sql, parameters).ConfigureAwait(false);

    public async Task<T[]> Query<T>(string command, IEnumerable<object>? parameters = null)
        => await this.Query<T>(command, Sql, parameters).ConfigureAwait(false);

    public async Task<T[]> Query<T>(string command, QueryLanguage language, params object[] parameters)
        => await this.Query<T>(command, language, (IEnumerable<object>)parameters).ConfigureAwait(false);

    public async Task<T[]> Query<T>(string command, QueryLanguage language, IEnumerable<object>? parameters = null)
        => await this.server.HttpExecute<T>("query", this.Database, command, parameters?.ToArray(), language).ConfigureAwait(false);

    public async Task<T[]> Execute<T>(string command, params object[] parameters)
        => await this.Execute<T>(command, Sql, parameters).ConfigureAwait(false);

    public async Task<T[]> Execute<T>(string command, IEnumerable<object>? parameters = null)
        => await this.Execute<T>(command, Sql, parameters).ConfigureAwait(false);

    public async Task<T[]> Execute<T>(string command, QueryLanguage language, params object[] parameters)
        => await this.Execute<T>(command, language, (IEnumerable<object>)parameters).ConfigureAwait(false);

    public async Task<T[]> Execute<T>(string command, QueryLanguage language, IEnumerable<object>? parameters = null)
        => await this.server.HttpExecute<T>("command", this.Database, command, parameters?.ToArray(), language).ConfigureAwait(false);
}
