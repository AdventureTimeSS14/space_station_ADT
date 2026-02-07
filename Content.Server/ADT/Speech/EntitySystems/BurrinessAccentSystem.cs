using Content.Server.Speech.Components;
using Content.Shared.Speech;
using Robust.Shared.Random;
using System.Text.RegularExpressions;

namespace Content.Server.Speech.EntitySystems;

public sealed class BurrinessAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BurrinessAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, BurrinessAccentComponent component, ref AccentGetEvent args)
    {
        var message = args.Message;
        message = Regex.Replace(message, "r+", "hh");
        message = Regex.Replace(message, "R+", "HH");
        message = Regex.Replace(message, "р+", "хх");
        message = Regex.Replace(message, "Р+", "ХХ");
        args.Message = message;
    }
}
