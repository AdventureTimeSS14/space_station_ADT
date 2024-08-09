using Content.Server.Popups;
using Content.Server.Tabletop;
using Content.Shared.ADT.GhostInteractions;
using Content.Server.Light.Components;
using Content.Server.Ghost;
using Content.Server.Tabletop.Components;
using Robust.Shared.Player;
using Content.Shared.Interaction;

namespace Content.Server.ADT.GhostInteractions;

// this does not support holding multiple translators at once yet.
// that should not be an issue for now, but it better get fixed later.
public sealed class OuijaBoardUserSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TabletopSystem _tabletop = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OuijaBoardUserComponent, UserActivateInWorldEvent>(OnInteract);
    }
    private void OnInteract(EntityUid uid, OuijaBoardUserComponent component, UserActivateInWorldEvent args)
    {
        if (args.Target == args.User || args.Target == null)
            return;
        var target = args.Target;

        if (HasComp<PoweredLightComponent>(target))
        {
            args.Handled = _ghost.DoGhostBooEvent(target);
            return;
        }
        if (HasComp<OuijaBoardComponent>(target) && HasComp<TabletopGameComponent>(target))
        {
            if (!TryComp<ActorComponent>(uid, out var actor))
                return;

            _tabletop.OpenSessionFor(actor.PlayerSession, target);
        }
    }

}
