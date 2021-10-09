using System.Net.Http;
using System.Text;

namespace ArcadeDb.Client;

public record CommandPayload(string Command, object Parameters, QueryLanguage Language)
{
    public StringContent ToStringContent() => new(new
    {
        language = this.Language.ToString().ToLower(),
        this.Command,
        serializer = "record",
        @params = this.Parameters
    }.ToJson(), Encoding.UTF8, "application/json");
}
