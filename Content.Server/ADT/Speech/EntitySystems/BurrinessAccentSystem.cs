using System.Text.RegularExpressions;
using Content.Server.Speech.Components;
using Content.Shared.Speech;
using Robust.Shared.Random;

namespace Content.Server.Speech.EntitySystems;

public sealed class BurrinessAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    private static readonly Regex BurrRegex = new Regex("[рР]+", RegexOptions.Compiled);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BurrinessAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, BurrinessAccentComponent component, AccentGetEvent args)
    {
        var message = args.Message;

        message = BurrRegex.Replace(
            message,
            match => _random.Pick(new List<string> { "хх" })
        );

        args.Message = message;
    }
}
