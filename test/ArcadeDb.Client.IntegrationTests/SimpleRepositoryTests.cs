using NodaTime.Testing;

namespace ArcadeDb.Client.IntegrationTests;

public class SimpleRepositoryTests : IDisposable
{
    [Fact]
    public async Task CanCreate()
    {
        var entity = new TestEntity("Banana", 12);
        var createdEntity = await this.target.Insert(entity);

        createdEntity.Rid.Should().NotBeEmpty();
        createdEntity.Name.Should().Be(entity.Name);
        createdEntity.Age.Should().Be(entity.Age);
        createdEntity.CreatedDate.Should().NotBeNull();
        createdEntity.UpdatedDate.Should().BeNull();
    }

    [Fact]
    public async Task CanGet()
    {
        var entity = new TestEntity("Banana", 12);
        var created = await this.target.Insert(entity);
        created.Rid.Should().NotBeNull();
        var createdEntity = await this.target.Get(created.Rid);

        createdEntity.Rid.Should().NotBeEmpty();
        createdEntity.Name.Should().Be(entity.Name);
        createdEntity.Age.Should().Be(entity.Age);
        createdEntity.CreatedDate.Should().NotBeNull();
        createdEntity.UpdatedDate.Should().BeNull();
    }

    private record TestEntity(string Name, int Age) : Entity;

    private readonly SimpleRepository<TestEntity> target;

    public SimpleRepositoryTests()
    {
        var database = new RemoteDatabase("http://localhost:2480", "_test", "root", "locallocal");
        database.Create();
        this.target = new SimpleRepository<TestEntity>(database, new FakeClock(SystemClock.Instance.GetCurrentInstant()));
    }


    public void Dispose()
    {
        this.target.Dispose();
    }
}
