namespace ArcadeDb.Client;

public class ArcadeDbException : Exception
{
    public ArcadeDbException(string? message) : base(message) { }
}

public class ParseException : Exception
{
    public ParseException(string? message) : base(message) { }
}
