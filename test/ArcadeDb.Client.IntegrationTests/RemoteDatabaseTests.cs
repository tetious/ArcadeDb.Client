namespace ArcadeDb.Client.IntegrationTests;

public class RemoteDatabaseTests : IDisposable
{
    [Fact]
    public async Task ParamsWork()
    {
        var result = await this.target.Execute<Movie>("select from Movie limit :count", new { count = 1 });
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task ParamsWork_Cypher()
    {
        var result = await this.target.Execute<Movie>("match (m:Movie) where m.title = $title return m", QueryLanguage.Cypher, new { title = "The Matrix" });
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task CanDeserialize()
    {
        var result = await this.target.Execute<Movie>("match (m:Movie) where m.title = $title return m", QueryLanguage.Cypher, new { title = "The Matrix" });
        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new Movie("The Matrix", "Welcome to the Real World", 1999));
    }

    [Fact]
    public async Task CanDeserialize_Sql()
    {
        var result = await this.target.Execute<Movie>("select from Movie where title = :title", new { title = "The Matrix" });
        result.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new Movie("The Matrix", "Welcome to the Real World", 1999));
    }

    [Fact]
    public async Task CreateAndDropWork()
    {
        this.target.Use("banana-pie");
        await this.target.Invoking(t => t.Create()).Should().NotThrowAsync();
        await this.target.Invoking(t => t.Drop()).Should().NotThrowAsync();
    }

    private readonly RemoteDatabase target;

    public RemoteDatabaseTests()
    {
        this.target = new RemoteDatabase("http://localhost:2480", "Movies", "root", "locallocal");
    }

    public void Dispose()
    {
        this.target.Dispose();
    }

    private record Movie(string Title, string Tagline, int Released);
}
