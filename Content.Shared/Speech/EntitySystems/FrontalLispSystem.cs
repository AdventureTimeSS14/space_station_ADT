using System.Text.RegularExpressions;
using Content.Shared.Speech.Components;
using Content.Shared.Random.Helpers; // Corvax-Localization
using Robust.Shared.Random; // Corvax-Localization
using Robust.Shared.Timing; // Corvax-Localization

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class FrontalLispSystem : RelayAccentSystem<FrontalLispComponent>
{
    // @formatter:off
    private static readonly Regex RegexUpperTh = new("[T]+[Ss]+|[S]+[Cc]+(?=[IiEeYy]+)|[C]+(?=[IiEeYy]+)|[P][Ss]+|([S]+[Tt]+|[T]+)(?=[Ii]+[Oo]+[Uu]*[Nn]*)|[C]+[Hh]+(?=[Ii]*[Ee]*)|[Z]+|[S]+|[X]+(?=[Ee]+)");
    private static readonly Regex RegexLowerTh = new("[t]+[s]+|[s]+[c]+(?=[iey]+)|[c]+(?=[iey]+)|[p][s]+|([s]+[t]+|[t]+)(?=[i]+[o]+[u]*[n]*)|[c]+[h]+(?=[i]*[e]*)|[z]+|[s]+|[x]+(?=[e]+)");
    private static readonly Regex RegexUpperEcks = new("[E]+[Xx]+[Cc]*|[X]+");
    private static readonly Regex RegexLowerEcks = new("[e]+[x]+[c]*|[x]+");
    // @formatter:on

    [Dependency] private IGameTiming _timing = default!; // Corvax-Localization
    [Dependency] private IRobustRandom _random = default!; // Corvax-Localization

    public override string Accentuate(string message, Entity<FrontalLispComponent>? ent = null)
    {
        // handles ts, sc(i|e|y), c(i|e|y), ps, st(io(u|n)), ch(i|e), z, s
        message = RegexUpperTh.Replace(message, "TH");
        message = RegexLowerTh.Replace(message, "th");
        // handles ex(c), x
        message = RegexUpperEcks.Replace(message, "EKTH");
        message = RegexLowerEcks.Replace(message, "ekth");

        // Corvax-Localization-Start
        var random = ent.HasValue
            ? SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent.Value))
            : _random;

        // с - ш
        message = Regex.Replace(message, @"с", random.Prob(0.90f) ? "ш" : "с");
        message = Regex.Replace(message, @"С", random.Prob(0.90f) ? "Ш" : "С");
        // ч - ш
        message = Regex.Replace(message, @"ч", random.Prob(0.90f) ? "ш" : "ч");
        message = Regex.Replace(message, @"Ч", random.Prob(0.90f) ? "Ш" : "Ч");
        // ц - ч
        message = Regex.Replace(message, @"ц", random.Prob(0.90f) ? "ч" : "ц");
        message = Regex.Replace(message, @"Ц", random.Prob(0.90f) ? "Ч" : "Ц");
        // т - ч
        message = Regex.Replace(message, @"\B[т](?![АЕЁИОУЫЭЮЯаеёиоуыэюя])", random.Prob(0.90f) ? "ч" : "т");
        message = Regex.Replace(message, @"\B[Т](?![АЕЁИОУЫЭЮЯаеёиоуыэюя])", random.Prob(0.90f) ? "Ч" : "Т");
        // з - ж
        message = Regex.Replace(message, @"з", random.Prob(0.90f) ? "ж" : "з");
        message = Regex.Replace(message, @"З", random.Prob(0.90f) ? "Ж" : "З");
        // Corvax-Localization-End

        return message;
    }
}
