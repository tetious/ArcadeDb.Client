namespace ArcadeDb.Client.IntegrationTests;

public class ArcadeDatabaseTests : IDisposable
{
    [Fact]
    public async Task ParamsWork()
    {
        var result = await this.server.Use("movies")
            .Execute<Movie>("select from Movie limit :count", new { count = 1 });
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task ParamsWork_Cypher()
    {
        var result = await this.server.Use("movies")
            .Execute<Movie>("match (m:Movie) where m.title = $title return m", QueryLanguage.Cypher, new { title = "The Matrix" });
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CanDeserialize()
    {
        var result = await this.server.Use("movies")
            .Execute<Movie>("match (m:Movie) where m.title = $title return m", QueryLanguage.Cypher, new { title = "The Matrix" });
        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new Movie("The Matrix", "Welcome to the Real World", 1999));
    }

    [Fact]
    public async Task CanDeserialize_Sql()
    {
        var result = await this.server.Use("movies").Execute<Movie>("select from Movie where title = :title", new { title = "The Matrix" });
        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new Movie("The Matrix", "Welcome to the Real World", 1999));
    }

    [Fact]
    public async Task TransactionsAreIsolated()
    {
        var db = this.server.Use("_test_movies");
        await db.Execute("create document type Movie");

        using var unit = await db.BeginTransaction();
        await unit.Execute("insert into Movie content {title: 'Pie Bandits'}");

        var movies = await unit.Query<Movie>("select from Movie where title = 'Pie Bandits'");
        movies.Should().ContainSingle(m => m.Title == "Pie Bandits");

        movies = await db.Query<Movie>("select from Movie where title = 'Pie Bandits'");
        movies.Should().BeEmpty();

        await unit.Commit();
        movies = await db.Query<Movie>("select from Movie where title = 'Pie Bandits'");
        movies.Should().ContainSingle(m => m.Title == "Pie Bandits");
    }

    [Fact]
    public async Task TransactionRollbacksWork()
    {
        var db = this.server.Use("_test_movies");
        await db.Execute("create document type Movie");

        using var unit = await db.BeginTransaction();

        await unit.Execute("insert into Movie content {title: 'Pie Bandits'}");
        var movies = await unit.Query<Movie>("select from Movie where title = 'Pie Bandits'");
        movies.Should().ContainSingle(m => m.Title == "Pie Bandits");

        await unit.Rollback();

        movies = await db.Query<Movie>("select from Movie where title = 'Pie Bandits'");
        movies.Should().BeEmpty();
    }

    private readonly ArcadeServer server;

    public ArcadeDatabaseTests()
    {
        this.server = new ArcadeServer("http://root:locallocal@localhost:2480");
        this.server.Create("_test_movies").Wait();
    }

    public void Dispose()
    {
        this.server.Drop("_test_movies").Wait();
        this.server.Dispose();
    }

    private record Movie(string Title, string Tagline, int Released);
}
