namespace ArcadeDb.Client;

public class ArcadeTransaction : ArcadeDatabaseHandle, IDisposable
{
    public bool IsDisposed { get; private set; }

    public ArcadeTransaction(ArcadeServer server, string database, string sessionId) : base(server, database, sessionId) { }

    public async Task Commit()
    {
        if (this.IsDisposed) throw new ObjectDisposedException(nameof(ArcadeTransaction));

        await this.Server.Commit(this.Database, this.SessionId!);
    }

    public async Task Rollback()
    {
        if (this.IsDisposed) throw new ObjectDisposedException(nameof(ArcadeTransaction));

        await this.Server.Rollback(this.Database, this.SessionId!);
    }

    public void Dispose()
    {
        this.IsDisposed = true;
    }
}
