using Content.Shared.ADT.SlimeHair;
using Robust.Shared.Player;

namespace Content.Server.ADT.SlimeHair;

/// <summary>
/// Allows humanoids to change their appearance mid-round.
/// </summary>
public sealed partial class SlimeHairSystem
{
    private void InitializeSlimeAbilities()
    {
        SubscribeLocalEvent<SlimeHairComponent, SlimeHairActionEvent>(SlimeHairAction);
    }

    private void SlimeHairAction(EntityUid uid, SlimeHairComponent comp, SlimeHairActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        _uiSystem.TryOpenUi(uid, SlimeHairUiKey.Key, actor.Owner);

        UpdateInterface(uid, comp);

        args.Handled = true;
    }
}
