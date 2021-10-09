namespace ArcadeDb.Client.IntegrationTests;

public class ArcadeServerTests : IDisposable
{
    [Fact]
    public async Task CreateAndDropWork()
    {
        await this.target.Invoking(t => t.Create("banana-pie")).Should().NotThrowAsync();
        await this.target.Invoking(t => t.Drop("banana-pie")).Should().NotThrowAsync();
    }

    [Fact]
    public async Task CanListDatabases()
    {
        var databases = await this.target.ListDatabases();
        databases.Should().HaveCountGreaterOrEqualTo(1).And.Contain("Movies");
    }

    private readonly ArcadeServer target;

    public ArcadeServerTests()
    {
        this.target = new ArcadeServer("http://localhost:2480", "root", "locallocal");
    }

    public void Dispose()
    {
        this.target.Dispose();
    }
}
