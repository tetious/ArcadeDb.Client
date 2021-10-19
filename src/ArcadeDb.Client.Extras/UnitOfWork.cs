namespace ArcadeDb.Client.Extras;

public interface IUnitOfWork
{
    Task<T[]> QuerySql<T>(string command, params object[] parameters)
        where T : Entity;

    Task<T[]> QueryCypher<T>(string command, params object[] parameters)
        where T : Entity;

    Task<T[]> ExecuteSql<T>(string command, params object[] parameters)
        where T : Entity;

    Task<T[]> ExecuteCypher<T>(string command, params object[] parameters)
        where T : Entity;

    Task<T?> Get<T>(Guid id)
        where T : Entity;

    Task<T> Insert<T>(T entity)
        where T : Entity;

    Task<T> Update<T>(T entity)
        where T : Entity;

    Task Delete<T>(Guid id)
        where T : Entity;
}

public class UnitOfWork : IDisposable, IUnitOfWork
{
    private readonly ArcadeTransaction arcadeTransaction;
    private readonly IClock clock;

    public UnitOfWork(ArcadeTransaction arcadeTransaction, IClock clock)
    {
        this.arcadeTransaction = arcadeTransaction;
        this.clock = clock;
    }

    public Task<T[]> QuerySql<T>(string command, params object[] parameters)
        where T : Entity =>
        this.arcadeTransaction.Query<T>(command, parameters);

    public Task<T[]> QueryCypher<T>(string command, params object[] parameters)
        where T : Entity =>
        this.arcadeTransaction.Query<T>(command, QueryLanguage.Cypher, parameters);

    public Task<T[]> ExecuteSql<T>(string command, params object[] parameters)
        where T : Entity =>
        this.arcadeTransaction.Execute<T>(command, parameters);

    public Task<T[]> ExecuteCypher<T>(string command, params object[] parameters)
        where T : Entity =>
        this.arcadeTransaction.Execute<T>(command, QueryLanguage.Cypher, parameters);

    public Task<T?> Get<T>(Guid id)
        where T : Entity =>
        new SimpleRepository<T>(this.arcadeTransaction, this.clock).Get(id);

    public Task<T> Insert<T>(T entity)
        where T : Entity =>
        new SimpleRepository<T>(this.arcadeTransaction, this.clock).Insert(entity);

    public Task<T> Update<T>(T entity)
        where T : Entity =>
        new SimpleRepository<T>(this.arcadeTransaction, this.clock).Update(entity);

    public Task Delete<T>(Guid id)
        where T : Entity =>
        new SimpleRepository<T>(this.arcadeTransaction, this.clock).Delete(id);

    public void Dispose()
    {
        this.arcadeTransaction.Dispose();
    }
}
