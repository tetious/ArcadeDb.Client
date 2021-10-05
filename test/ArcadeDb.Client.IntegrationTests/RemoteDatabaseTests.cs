using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace ArcadeDb.Client.IntegrationTests;

public class RemoteDatabaseTests: IDisposable
{
    [Fact]
    public async Task ParamsWork()
    {
        var result = await this.target.Command("select from Movie limit :count", new { count = 1 });
        result.GetProperty("result").EnumerateArray().Should().HaveCount(1);
    }

    [Fact]
    public async Task ParamsWork_Cypher()
    {
        var result = await this.target.Command("match (m:Movie) where m.title = $title return m", new { title = "The Matrix" }, QueryLanguage.Cypher);
        result.GetProperty("result").EnumerateArray().Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateAndDropWork()
    {
        this.target.Use("banana-pie");
        var result = await this.target.Create();
        result.IsError.Should().BeFalse();
        result = await this.target.Drop();
        result.IsError.Should().BeFalse();
    }

    private readonly RemoteDatabase target;

    public RemoteDatabaseTests()
    {
        this.target = new RemoteDatabase("http://localhost:2480", "Movies", "root", "locallocal");
    }

    public void Dispose()
    {
        this.target.Drop("banana-pie");

        this.target.Dispose();
    }
}
