// Inspired by Nyanotrasen
using Content.Shared.ADT.CharecterFlavor;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client.ADT.CharecterFlavor;

public sealed class CharecterFlavorSystem : SharedCharecterFlavorSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<SetHeadshotUiMessage>(OnSetHeadshot);
    }

    private void OnSetHeadshot(SetHeadshotUiMessage args)
    {
        var controller = _ui.GetUIController<CharacterFlavorUiController>();
        controller.SetHeadshot(args.Target, args.Image);
    }

    protected override void OpenFlavor(EntityUid actor, EntityUid target)
    {
        base.OpenFlavor(actor, target);

        if (!_timing.IsFirstTimePredicted)
            return;

        if (!HasComp<CharacterFlavorComponent>(target))
            return;

        var controller = _ui.GetUIController<CharacterFlavorUiController>();
        controller.OpenMenu(target);
    }
}
