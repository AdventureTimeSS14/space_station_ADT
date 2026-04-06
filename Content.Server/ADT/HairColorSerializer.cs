using System.Linq;
using System.Text.Json;
using Content.Shared.Humanoid;
using Robust.Shared.Utility;

namespace Content.Server.ADT;

/// <summary>
/// Helper methods for serializing hair colors to/from database storage.
/// </summary>
public static class HairColorSerializer
{
    /// <summary>
    /// Serializes a list of hair colors to a JSON array string for database storage.
    /// </summary>
    public static string Serialize(List<Color> hairColors)
    {
        if (hairColors == null || hairColors.Count == 0)
            return "[]";

        var hexColors = hairColors.Select(c => c.ToHex()).ToArray();
        return JsonSerializer.Serialize(hexColors);
    }

    /// <summary>
    /// Parses a JSON array string of hair colors from database storage.
    /// Falls back to legacy single color format if parsing fails.
    /// </summary>
    public static List<Color> Deserialize(string hairColorData)
    {
        if (string.IsNullOrEmpty(hairColorData))
            return new List<Color> { Color.Black };

        try
        {
            var hexColors = JsonSerializer.Deserialize<string[]>(hairColorData);
            if (hexColors != null && hexColors.Length > 0)
                return hexColors.Select(hex => Color.FromHex(hex)).ToList();
        }
        catch
        {
            // Fall back to legacy single color format
        }

        return new List<Color> { Color.FromHex(hairColorData) };
    }
}
