namespace ArcadeDb.Client;

public class ArcadeDatabase : ArcadeDatabaseHandle
{
    public ArcadeDatabase(ArcadeServer server, string database) : base(server, database) { }

    public async Task<ArcadeTransaction> BeginTransaction() => new ArcadeTransaction(this.Server, this.Database, await this.Server.Begin(this.Database));
}
