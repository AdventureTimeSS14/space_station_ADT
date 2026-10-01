using System.Text.RegularExpressions;
using Content.Shared.Speech.Components;

namespace Content.Shared.Speech.EntitySystems;

public sealed partial class MothAccentSystem : RelayAccentSystem<MothAccentComponent>
{
    private static readonly Regex RegexLowerBuzz = new("z{1,3}");
    private static readonly Regex RegexUpperBuzz = new("Z{1,3}");

    public override string Accentuate(string message, Entity<MothAccentComponent>? ent = null)
    {
        // buzzz
        message = RegexLowerBuzz.Replace(message, "zzz");
        // buZZZ
        message = RegexUpperBuzz.Replace(message, "ZZZ");

        // ADT-Tweak-Start
        message = Regex.Replace(message, "з{1,3}", "ззз");
        message = Regex.Replace(message, "с{1,3}", "зз");
        message = Regex.Replace(message, "ц{1,3}", "зз");
        message = Regex.Replace(message, "ж{1,3}", "жзж");
        message = Regex.Replace(message, "З{1,3}", "ЗЗЗ");
        message = Regex.Replace(message, "С{1,3}", "ЗЗ");
        message = Regex.Replace(message, "Ц{1,3}", "ЗЗ");
        message = Regex.Replace(message, "Ж{1,3}", "ЖЗЖ");
        // ADT-Tweak-End

        return message;
    }
}
