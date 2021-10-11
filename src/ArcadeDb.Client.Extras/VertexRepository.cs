using System.Reflection;
using System.Text.Json;

namespace ArcadeDb.Client.Extras;

public interface IVertexRepository<T> : ISimpleRepository<T>
    where T : Entity.Vertex
{
    Task<(TEdge Edge, TOther Other)[]> GetIn<TEdge, TOther>(RecordId id)
        where TEdge : Edge
        where TOther : Entity.Vertex;

    Task<(TEdge Edge, TOther Other)[]> GetOut<TEdge, TOther>(RecordId id)
        where TEdge : Edge
        where TOther : Entity.Vertex;
}

public class VertexRepository<T> : SimpleRepository<T>, IVertexRepository<T>
    where T : Entity.Vertex
{
    public VertexRepository(ArcadeDatabase database, IClock clock) : base(database, clock) { }

    public async Task<(TEdge Edge, TOther Other)[]> GetIn<TEdge, TOther>(RecordId id)
        where TEdge : Edge
        where TOther : Entity.Vertex =>
        await this.GetRelated<TEdge, TOther>(id, true, false);

    public async Task<(TEdge Edge, TOther Other)[]> GetOut<TEdge, TOther>(RecordId id)
        where TEdge : Edge
        where TOther : Entity.Vertex =>
        await this.GetRelated<TEdge, TOther>(id, false, true);

    private async Task<(TEdge Edge, TOther Other)[]> GetRelated<TEdge, TOther>(RecordId id, bool getIn, bool getOut)
        where TEdge : Edge
        where TOther : Entity.Vertex
    {
        var edgeType = typeof(TEdge).Name.ToSnakeCase().ToUpper();
        var otherTypeName = typeof(TOther).Name;

        // both true, both empty
        // in true, in <, out empty
        // out true, in empty, out >
        var (aIn, aOut) = (string.Empty, string.Empty);
        if (getIn && getOut == false) aIn = "<";
        if (getOut && getIn == false) aOut = ">";
        var query = $"MATCH (m:{EntityName}){aIn}-[i:{edgeType}]-{aOut}(o:{otherTypeName}) WHERE id(m) = '{id}' RETURN i, o";

        var results = await this.Database.Query<JsonElement>(query, QueryLanguage.Cypher);
        return results.Select(r =>
        {
            var edge = r.GetProperty("i").Deserialize<TEdge>(Json.DefaultSerializerOptions);
            var other = r.GetProperty("o").Deserialize<TOther>(Json.DefaultSerializerOptions);
            return (edge!, other!);
        }).ToArray();
    }
}
