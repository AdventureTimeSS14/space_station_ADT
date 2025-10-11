using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;
using Content.Shared.Speech;

namespace Content.Server.Speech.EntitySystems;

public sealed class MothAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!; // Corvax-Localization

    private static readonly Regex RegexLowerBuzz = new Regex("z{1,3}");
    private static readonly Regex RegexUpperBuzz = new Regex("Z{1,3}");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MothAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, MothAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        // re edited by Tosti, SD-tweak-start

        message = Regex.Replace(message, "з{1,3}", "зз");

        message = Regex.Replace(message, "с{1,3}", "с");

        message = Regex.Replace(message, "ц{1,3}", "ц");

        message = Regex.Replace(message, "ж{1,3}", "жж");

        message = Regex.Replace(message, "З{1,3}", "ЗЗ");

        message = Regex.Replace(message, "С{1,3}", "С");

        message = Regex.Replace(message, "Ц{1,3}", "Ц");

        message = Regex.Replace(message, "Ж{1,3}", "ЖЖ");

        // re edited by Tosti, SD-tweak-end

        args.Message = message;
    }
}
