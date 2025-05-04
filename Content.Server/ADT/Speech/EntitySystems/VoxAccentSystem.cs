using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class VoxAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!; // Corvax-Localization

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VoxAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, VoxAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;
        // ADT-Localization-Start
        // к => ке
        message = Regex.Replace(
            message,
            "к+",
            _random.Pick(new List<string>() { "ки", "кик" })
        );
        // К => Ке
        message = Regex.Replace(
            message,
            "К+",
            _random.Pick(new List<string>() { "Ки", "Кик" })
        );
        // ADT-Localization-End
        message = Regex.Replace(message, "ч+", "ч");
        message = Regex.Replace(message, "Ч+", "Ч");

        args.Message = message;
    }
}