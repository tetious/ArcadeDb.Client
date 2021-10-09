using System.Text.RegularExpressions;

namespace ArcadeDb.Client;

public static class ProtocolHelpers
{
    private static readonly Regex RidRegex = new(@"^#\d+:\d+$", RegexOptions.Compiled);

    public static bool IsRecordId(string candidate) => RidRegex.IsMatch(candidate);
}
