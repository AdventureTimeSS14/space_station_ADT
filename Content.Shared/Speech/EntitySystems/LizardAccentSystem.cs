using System.Text.RegularExpressions;
using Content.Shared.Random.Helpers;
using Content.Shared.Speech.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class LizardAccentSystem : RelayAccentSystem<LizardAccentComponent>
{
    private static readonly Regex RegexLowerS = new("s+");
    private static readonly Regex RegexUpperS = new("S+");
    private static readonly Regex RegexInternalX = new(@"(\w)x");
    private static readonly Regex RegexLowerEndX = new(@"\bx([\-|r|R]|\b)");
    private static readonly Regex RegexUpperEndX = new(@"\bX([\-|r|R]|\b)");

    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override string Accentuate(string message, Entity<LizardAccentComponent>? ent = null)
    {
        // ADT-Tweak-Start
        var random = ent.HasValue
            ? SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent.Value))
            : _random;
        // ADT-Tweak-End
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
            random.Pick(new List<string>() { "сс", "ссс" })
        );
        // С => CCC
        message = Regex.Replace(
            message,
            "С+",
            random.Pick(new List<string>() { "СС", "ССС" })
        );
        // з => ссс
        message = Regex.Replace(
            message,
            "з+",
            random.Pick(new List<string>() { "сс", "ссс" })
        );
        // З => CCC
        message = Regex.Replace(
            message,
            "З+",
            random.Pick(new List<string>() { "СС", "ССС" })
        );
        // ш => шшш
        message = Regex.Replace(
            message,
            "ш+",
            random.Pick(new List<string>() { "шш", "шшш" })
        );
        // Ш => ШШШ
        message = Regex.Replace(
            message,
            "Ш+",
            random.Pick(new List<string>() { "ШШ", "ШШШ" })
        );
        // ч => щщщ
        message = Regex.Replace(
            message,
            "ч+",
            random.Pick(new List<string>() { "щщ", "щщщ" })
        );
        // Ч => ЩЩЩ
        message = Regex.Replace(
            message,
            "Ч+",
            random.Pick(new List<string>() { "ЩЩ", "ЩЩЩ" })
        );
        // Corvax-Localization-End
        return message;
    }
}
