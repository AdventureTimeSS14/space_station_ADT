using System.Linq;
using Content.Server.DoAfter;
using Content.Server.Humanoid;
using Content.Shared.UserInterface;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Interaction;
using Content.Shared.SlimeHair;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Content.Server.Actions;

namespace Content.Server.SlimeHair;

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

        _uiSystem.OpenUi(uid, SlimeHairUiKey.Key, actor.PlayerSession);

        UpdateInterface(uid, comp);

        args.Handled = true;
    }
}
