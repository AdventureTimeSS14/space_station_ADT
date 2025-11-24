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
        SubscribeLocalEvent<MidroundCustomizationComponent, SlimeHairActionEvent>(SlimeHairAction);
    }

    private void SlimeHairAction(EntityUid uid, MidroundCustomizationComponent comp, SlimeHairActionEvent args)
    {
        if (args.Handled)
            return;

        _uiSystem.TryToggleUi(uid, SlimeHairUiKey.Key, uid);
        UpdateInterface(uid, comp);

        args.Handled = true;
    }
}
