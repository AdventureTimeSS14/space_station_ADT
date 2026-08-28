using System.Globalization;

namespace Content.Client.ADT.TTS;

public static class TTSRadioVolumes
{
    public const float FullVolume = 1f;

    private const char PairSeparator = ';';
    private const char ValueSeparator = '=';

    public static Dictionary<string, float> Parse(string raw)
    {
        var volumes = new Dictionary<string, float>();

        if (string.IsNullOrWhiteSpace(raw))
            return volumes;

        foreach (var pair in raw.Split(PairSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf(ValueSeparator);
            if (separator <= 0)
                continue;

            var channel = pair[..separator].Trim();
            var value = pair[(separator + 1)..];

            if (channel.Length == 0 ||
                !float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var volume))
            {
                continue;
            }

            volumes[channel] = Math.Clamp(volume, 0f, FullVolume);
        }

        return volumes;
    }

    public static string Serialize(IEnumerable<KeyValuePair<string, float>> volumes)
    {
        var parts = new List<string>();

        foreach (var (channel, volume) in volumes)
        {
            var clamped = Math.Clamp(volume, 0f, FullVolume);

            if (clamped >= FullVolume)
                continue;

            parts.Add($"{channel}{ValueSeparator}{clamped.ToString("0.###", CultureInfo.InvariantCulture)}");
        }

        parts.Sort(StringComparer.Ordinal);
        return string.Join(PairSeparator, parts);
    }
}
