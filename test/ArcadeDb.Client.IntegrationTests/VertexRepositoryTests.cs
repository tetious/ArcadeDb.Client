using NodaTime.Testing;

namespace ArcadeDb.Client.IntegrationTests;

public class VertexRepositoryTests : IDisposable
{
    [Fact]
    public async Task CanQueryCypher()
    {
        var movies = await this.movieRepository.QueryCypher("match (m:Movie {title: $title}) return m", new { title = "The Matrix" });
        var movie = movies.Should().ContainSingle().Which;

        movie.RecordId.Should().NotBe(default);
        movie.Title.Should().Be("The Matrix");
        movie.Tagline.Should().Be("Welcome to the Real World");
        movie.Released.Should().Be(1999);
    }

    [Fact]
    public async Task CanGetActorsViaActedIn()
    {
        var movies = await this.movieRepository.QueryCypher("match (m:Movie {title: $title}) return m", new { title = "The Matrix" });
        var movie = movies.Should().ContainSingle().Which;

        var actedInList = await this.movieRepository.GetIn<ActedIn, Person>(movie.RecordId);
        actedInList.Should().HaveCount(5);
        actedInList.Should().ContainSingle(e => e.Edge.Roles.Contains("Neo")).Subject.Other.Name.Should().Be("Keanu Reeves");
    }

    [Fact]
    public async Task CanGetMoviesViaActedIn()
    {
        var people = await this.personRepository.QueryCypher("match (p:Person {name: $name}) return p", new { name = "Tom Hanks" });
        var person = people.Should().ContainSingle().Which;

        var actedInList = await this.personRepository.GetOut<ActedIn, Movie>(person.RecordId);
        actedInList.Should().HaveCount(12);
        actedInList.Should().Contain(e => e.Edge.Roles.Contains("Paul Edgecomb")).Subject.Other.Title.Should().Be("The Green Mile");
    }

    private readonly ArcadeServer server;
    private readonly FakeClock clock = new(SystemClock.Instance.GetCurrentInstant());
    private readonly IVertexRepository<Movie> movieRepository;
    private readonly IVertexRepository<Person> personRepository;

    public VertexRepositoryTests()
    {
        this.server = new ArcadeServer("http://root:locallocal@localhost:2480");
        this.movieRepository = new VertexRepository<Movie>(this.server.Use("movies"), this.clock);
        this.personRepository = new VertexRepository<Person>(this.server.Use("movies"), this.clock);
    }

    private record Movie(string Title, string Tagline, int Released) : Entity.Vertex;

    private record Person(string Name) : Entity.Vertex;

    // FIXME: Roles is supposed to be an array, but the data is borked ATM.
    private record ActedIn(string Roles) : Edge;

    public void Dispose()
    {
        this.server.Dispose();
    }
}
