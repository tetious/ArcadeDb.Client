using System.Text;

namespace ArcadeDb.Client.Extras;

public static class StringExtensions
{
    public static string ToCamelCase(this string str, bool invariantCulture = true)
    {
        if (string.IsNullOrWhiteSpace(str)) return str;
        if (str.Length == 1) return invariantCulture ? str.ToLowerInvariant() : str.ToLower();
        return (invariantCulture ? char.ToLowerInvariant(str[0]) : char.ToLower(str[0])) + str[1..];
    }

    public static string ToSnakeCase(this string text)
    {
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (text.Length < 2) return text;

        var sb = new StringBuilder();
        sb.Append(char.ToLowerInvariant(text[0]));
        for (var i = 1; i < text.Length; ++i)
        {
            var c = text[i];
            if (char.IsUpper(c))
            {
                sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }
}
