using Content.Shared.ADT.MidroundCustomization;
using Robust.Shared.Player;

namespace Content.Server.ADT.MidroundCustomization;

/// <summary>
/// Allows humanoids to change their appearance mid-round.
/// </summary>
public sealed partial class MidroundCustomizationSystem
{
    private void InitializeAbilities()
    {
        SubscribeLocalEvent<MidroundCustomizationComponent, MidroundCustomizationActionEvent>(SlimeHairAction);
    }

    private void SlimeHairAction(EntityUid uid, MidroundCustomizationComponent comp, MidroundCustomizationActionEvent args)
    {
        if (args.Handled)
            return;

        _uiSystem.TryToggleUi(uid, MidroundCustomizationUiKey.Key, uid);
        UpdateInterface(uid, comp);

        args.Handled = true;
    }
}
