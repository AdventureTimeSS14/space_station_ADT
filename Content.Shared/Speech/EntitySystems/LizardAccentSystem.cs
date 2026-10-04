using System.Text.RegularExpressions;
using Content.Shared.Speech.Components;
using Robust.Shared.Random;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class LizardAccentSystem : RelayAccentSystem<LizardAccentComponent>
{
    private static readonly Regex RegexLowerS = new("s+");
    private static readonly Regex RegexUpperS = new("S+");
    private static readonly Regex RegexInternalX = new(@"(\w)x");
    private static readonly Regex RegexLowerEndX = new(@"\bx([\-|r|R]|\b)");
    private static readonly Regex RegexUpperEndX = new(@"\bX([\-|r|R]|\b)");

    [Dependency] private IRobustRandom _random = default!; // Corvax-Localization

    public override string Accentuate(string message, Entity<LizardAccentComponent>? ent = null)
    {
        // hissss
        message = RegexLowerS.Replace(message, "sss");
        // hiSSS
        message = RegexUpperS.Replace(message, "SSS");
        // ekssit
        message = RegexInternalX.Replace(message, "$1kss");
        // ecks
        message = RegexLowerEndX.Replace(message, "ecks$1");
        // eckS
        message = RegexUpperEndX.Replace(message, "ECKS$1");

        // Corvax-Localization-Start
        // c => ссс
        message = Regex.Replace(
            message,
            "с+",
            _random.Pick(new List<string>() { "сс", "ссс" })
        );
        // С => CCC
        message = Regex.Replace(
            message,
            "С+",
            _random.Pick(new List<string>() { "СС", "ССС" })
        );
        // з => ссс
        message = Regex.Replace(
            message,
            "з+",
            _random.Pick(new List<string>() { "сс", "ссс" })
        );
        // З => CCC
        message = Regex.Replace(
            message,
            "З+",
            _random.Pick(new List<string>() { "СС", "ССС" })
        );
        // ш => шшш
        message = Regex.Replace(
            message,
            "ш+",
            _random.Pick(new List<string>() { "шш", "шшш" })
        );
        // Ш => ШШШ
        message = Regex.Replace(
            message,
            "Ш+",
            _random.Pick(new List<string>() { "ШШ", "ШШШ" })
        );
        // ч => щщщ
        message = Regex.Replace(
            message,
            "ч+",
            _random.Pick(new List<string>() { "щщ", "щщщ" })
        );
        // Ч => ЩЩЩ
        message = Regex.Replace(
            message,
            "Ч+",
            _random.Pick(new List<string>() { "ЩЩ", "ЩЩЩ" })
        );
        // Corvax-Localization-End
        return message;
    }
}
