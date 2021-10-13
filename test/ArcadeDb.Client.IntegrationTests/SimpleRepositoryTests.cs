using Bogus;
using NodaTime.Testing;

namespace ArcadeDb.Client.IntegrationTests;

public class SimpleRepositoryTests : IDisposable
{
    [Fact]
    public async Task CanCreate()
    {
        var entity = new SimpleTestEntity("Banana", 12);
        var createdEntity = await this.target.Insert(entity);

        createdEntity.RecordId.Should().NotBeNull();
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

        createdEntity.RecordId.Should().NotBeNull();
        createdEntity.SubDoc.Name.Should().Be(entity.SubDoc.Name);
        createdEntity.SubDoc.Age.Should().Be(entity.SubDoc.Age);
        createdEntity.PieFlavors.Should().BeEquivalentTo(entity.PieFlavors);
        createdEntity.CreatedDate.Should().NotBeNull();
        createdEntity.UpdatedDate.Should().BeNull();
    }

    [Fact]
    public async Task CanUpdate()
    {
        var entity = new SimpleTestEntity("Banana", 12);
        var createdEntity = await this.target.Insert(entity);
        createdEntity.Name.Should().Be(entity.Name);
        createdEntity.Age.Should().Be(entity.Age);
        createdEntity.CreatedDate.Should().NotBeNull();
        createdEntity.UpdatedDate.Should().BeNull();

        entity = new SimpleTestEntity("Apple", 12);
        var updatedEntity = await this.target.Update(entity);

        updatedEntity.Age.Should().Be(entity.Age);
        updatedEntity.Name.Should().Be(entity.Name);
        updatedEntity.UpdatedDate.Should().NotBeNull();
    }

    [Fact]
    public async Task CanGet_RecordId()
    {
        var entity = new SimpleTestEntity("Banana", 12);
        var created = await this.target.Insert(entity);
        created.RecordId.Should().NotBe(default);
        var createdEntity = await this.target.Get(created.RecordId);

        createdEntity.Should().NotBeNull();
        createdEntity.RecordId.Should().NotBeNull();
        createdEntity.Name.Should().Be(entity.Name);
        createdEntity.Age.Should().Be(entity.Age);
        createdEntity.CreatedDate.Should().NotBeNull();
        createdEntity.UpdatedDate.Should().BeNull();
    }

    [Fact]
    public async Task CanQuery_Property()
    {
        var entity = new SimpleTestEntity(Faker.Company.CatchPhrase(), Faker.Random.Number());
        var created = await this.target.Insert(entity);
        created.RecordId.Should().NotBe(default);

        var results = await this.target.Query("name=:name", new { entity.Name });
        var queried = results.Should().ContainSingle().Which;

        queried.Should().NotBeNull();
        created.RecordId.Should().NotBe(default);
        queried.Name.Should().Be(entity.Name);
        queried.Age.Should().Be(entity.Age);
        queried.CreatedDate.Should().NotBeNull();
        queried.UpdatedDate.Should().BeNull();
    }

    private record SimpleTestEntity(string Name, int Age) : Entity.Document;

    private record ComplexTestEntity(string[] PieFlavors, SimpleTestEntity SubDoc) : Entity.Vertex;

    private readonly SimpleRepository<SimpleTestEntity> target;
    private readonly SimpleRepository<ComplexTestEntity> complexTarget;
    private readonly ArcadeServer server;
    private static readonly Faker Faker = new();

    public SimpleRepositoryTests()
    {
        this.server = new ArcadeServer("http://root:locallocal@localhost:2480");
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
