using Content.Shared.Alert;
using Content.Shared.Inventory;
using Content.Shared.Strip.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Strip;

public sealed partial class ThievingSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ThievingComponent, BeforeStripEvent>(OnBeforeStrip);
        SubscribeLocalEvent<ThievingComponent, InventoryRelayedEvent<BeforeStripEvent>>((e, c, ev) =>
            OnBeforeStrip(e, c, ev.Args));
        SubscribeLocalEvent<ThievingComponent, ToggleThievingEvent>(OnToggleStealthy);
        SubscribeLocalEvent<ThievingComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<ThievingComponent, ComponentRemove>(OnCompRemoved);
    }

    private void OnBeforeStrip(EntityUid uid, ThievingComponent component, BeforeStripEvent args)
    {
        args.Stealth |= component.Stealthy;
        if (args.Stealth)
        {
            args.Additive -= component.StripTimeReduction;
        }
    }

    private void OnCompInit(Entity<ThievingComponent> entity, ref ComponentInit args)
    {
        if (!entity.Comp.HideStealthyAlert) // ADT-Tweak 
            _alertsSystem.ShowAlert(entity.Owner, entity.Comp.StealthyAlertProtoId, 1);
    }

    private void OnCompRemoved(Entity<ThievingComponent> entity, ref ComponentRemove args)
    {
        if (!entity.Comp.HideStealthyAlert) // ADT-Tweak 
            _alertsSystem.ClearAlert(entity.Owner, entity.Comp.StealthyAlertProtoId);
    }

    private void OnToggleStealthy(Entity<ThievingComponent> ent, ref ToggleThievingEvent args)
    {
        if (args.Handled)
            return;

        // ADT-Tweak start
        if (ent.Comp.HideStealthyAlert)
        {
            args.Handled = true;
            return;
        }
        // ADT-Tweak end

        ent.Comp.Stealthy = !ent.Comp.Stealthy;
        _alertsSystem.ShowAlert(ent.Owner, ent.Comp.StealthyAlertProtoId, (short)(ent.Comp.Stealthy ? 1 : 0));
        DirtyField(ent.AsNullable(), nameof(ent.Comp.Stealthy), null);

        args.Handled = true;
    }
}