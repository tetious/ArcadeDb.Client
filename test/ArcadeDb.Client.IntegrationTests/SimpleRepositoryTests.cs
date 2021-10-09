using NodaTime.Testing;

namespace ArcadeDb.Client.IntegrationTests;

public class SimpleRepositoryTests : IDisposable
{
    [Fact]
    public async Task CanCreate()
    {
        var entity = new SimpleTestEntity("Banana", 12);
        var createdEntity = await this.target.Insert(entity);

        createdEntity.Rid.Should().NotBeEmpty();
        createdEntity.Name.Should().Be(entity.Name);
        createdEntity.Age.Should().Be(entity.Age);
        createdEntity.CreatedDate.Should().NotBeNull();
        createdEntity.UpdatedDate.Should().BeNull();
    }

    [Fact]
    public async Task CanCreate_Complex()
    {
        var entity = new ComplexTestEntity(new[] { "banana", "chocolate" }, new SimpleTestEntity("Banana", 12));
        var createdEntity = await this.complexTarget.Insert(entity);

        createdEntity.Rid.Should().NotBeEmpty();
        createdEntity.SubDoc.Name.Should().Be(entity.SubDoc.Name);
        createdEntity.SubDoc.Age.Should().Be(entity.SubDoc.Age);
        createdEntity.PieFlavors.Should().BeEquivalentTo(entity.PieFlavors);
        createdEntity.CreatedDate.Should().NotBeNull();
        createdEntity.UpdatedDate.Should().BeNull();
    }

    [Fact]
    public async Task CanGet_RecordId()
    {
        var entity = new SimpleTestEntity("Banana", 12);
        var created = await this.target.Insert(entity);
        created.Rid.Should().NotBeNull();
        var createdEntity = await this.target.Get(created.Rid);

        createdEntity.Should().NotBeNull();
        createdEntity.Rid.Should().NotBeEmpty();
        createdEntity.Name.Should().Be(entity.Name);
        createdEntity.Age.Should().Be(entity.Age);
        createdEntity.CreatedDate.Should().NotBeNull();
        createdEntity.UpdatedDate.Should().BeNull();
    }

    [Fact]
    public async Task CanGet_Id()
    {
        var entity = new SimpleTestEntity("Banana", 12);
        var created = await this.target.Insert(entity);
        created.Id.Should().NotBeEmpty();
        var createdEntity = await this.target.Get(created.Id);

        createdEntity.Should().NotBeNull();
        createdEntity.Rid.Should().NotBeEmpty();
        createdEntity.Name.Should().Be(entity.Name);
        createdEntity.Age.Should().Be(entity.Age);
        createdEntity.CreatedDate.Should().NotBeNull();
        createdEntity.UpdatedDate.Should().BeNull();
    }

    private record SimpleTestEntity(string Name, int Age) : Entity;

    private record ComplexTestEntity(string[] PieFlavors, SimpleTestEntity SubDoc) : Entity;

    private readonly SimpleRepository<SimpleTestEntity> target;
    private readonly SimpleRepository<ComplexTestEntity> complexTarget;
    private readonly ArcadeServer server;

    public SimpleRepositoryTests()
    {
        this.server = new ArcadeServer("http://localhost:2480", "root", "locallocal");
        this.server.Create("_test");
        var database = this.server.Use("_test");

        this.target = new SimpleRepository<SimpleTestEntity>(database, new FakeClock(SystemClock.Instance.GetCurrentInstant()));
        this.complexTarget = new SimpleRepository<ComplexTestEntity>(database, new FakeClock(SystemClock.Instance.GetCurrentInstant()));
    }


    public void Dispose()
    {
        this.server.Dispose();
    }
}
