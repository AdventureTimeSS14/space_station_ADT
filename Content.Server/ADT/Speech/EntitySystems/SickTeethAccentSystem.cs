using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class SickTeethAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SickTeethAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, SickTeethAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        message = Regex.Replace(
            message,
            "[рР]+",
            match => _random.Pick(new List<string> { "в", "ф", "р" })
        );

        message = Regex.Replace(
            message,
            "[лЛ]+",
            match => _random.Pick(new List<string> { "ф", "л", "ль" })
        );

        message = Regex.Replace(
            message,
            "[сС]+",
            match => _random.Pick(new List<string> { "ф", "с", "ш" })
        );

        message = Regex.Replace(
            message,
            "[тТ]+",
            match => _random.Pick(new List<string> { "ф", "т", "д" })
        );

        message = Regex.Replace(
            message,
            "[нН]+",
            match => _random.Pick(new List<string> { "м", "ф", "н" })
        );

        args.Message = message;
    }
}