namespace ArcadeDb.Client.Extras;

public static class StringExtensions
{
    public static string ToCamelCase(this string str, bool invariantCulture = true)
    {
        if (string.IsNullOrWhiteSpace(str)) return str;
        if (str.Length == 1) return invariantCulture ? str.ToLowerInvariant() : str.ToLower();
        return (invariantCulture ? char.ToLowerInvariant(str[0]) : char.ToLower(str[0])) + str[1..];
    }
}
