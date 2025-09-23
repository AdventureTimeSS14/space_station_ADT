using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Inventory.Events;
using Content.Shared.Rounding;
using Content.Shared.Toggleable;
using Robust.Shared.Timing;
using Content.Shared.ADT.Eye.Blinding;

namespace Content.Shared.ADT.ThermalVision;

public abstract class SharedThermalVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ThermalVisionComponent, ComponentStartup>(OnThermalVisionStartup);
        SubscribeLocalEvent<ThermalVisionComponent, MapInitEvent>(OnThermalVisionMapInit);
        SubscribeLocalEvent<ThermalVisionComponent, AfterAutoHandleStateEvent>(OnThermalVisionAfterHandle);
        SubscribeLocalEvent<ThermalVisionComponent, ComponentRemove>(OnThermalVisionRemove);

        SubscribeLocalEvent<ThermalVisionItemComponent, GetItemActionsEvent>(OnThermalVisionItemGetActions);
        SubscribeLocalEvent<ThermalVisionItemComponent, ToggleActionEvent>(OnThermalVisionItemToggle);
        SubscribeLocalEvent<ThermalVisionItemComponent, GotEquippedEvent>(OnThermalVisionItemGotEquipped);
        SubscribeLocalEvent<ThermalVisionItemComponent, GotUnequippedEvent>(OnThermalVisionItemGotUnequipped);
        SubscribeLocalEvent<ThermalVisionItemComponent, ActionRemovedEvent>(OnThermalVisionItemActionRemoved);
        SubscribeLocalEvent<ThermalVisionItemComponent, ComponentRemove>(OnThermalVisionItemRemove);
        SubscribeLocalEvent<ThermalVisionItemComponent, EntityTerminatingEvent>(OnThermalVisionItemTerminating);
    }

    private void OnThermalVisionStartup(Entity<ThermalVisionComponent> ent, ref ComponentStartup args)
    {
        ThermalVisionChanged(ent);
    }

    private void OnThermalVisionAfterHandle(Entity<ThermalVisionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        ThermalVisionChanged(ent);
    }

    private void OnThermalVisionMapInit(Entity<ThermalVisionComponent> ent, ref MapInitEvent args)
    {
        UpdateAlert(ent);
    }

    private void OnThermalVisionRemove(Entity<ThermalVisionComponent> ent, ref ComponentRemove args)
    {
        ThermalVisionRemoved(ent);
    }

    private void OnThermalVisionItemGetActions(Entity<ThermalVisionItemComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.InHands || !ent.Comp.Toggleable)
            return;

        if (ent.Comp.SlotFlags != args.SlotFlags)
            return;

        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
    }

    private void OnThermalVisionItemToggle(Entity<ThermalVisionItemComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ToggleThermalVisionItem(ent, args.Performer);
    }

    private void OnThermalVisionItemGotEquipped(Entity<ThermalVisionItemComponent> ent, ref GotEquippedEvent args)
    {
        if (ent.Comp.SlotFlags != args.SlotFlags)
            return;

        EnableThermalVisionItem(ent, args.Equipee);
    }

    private void OnThermalVisionItemGotUnequipped(Entity<ThermalVisionItemComponent> ent, ref GotUnequippedEvent args)
    {
        if (ent.Comp.SlotFlags != args.SlotFlags)
            return;

        DisableThermalVisionItem(ent, args.Equipee);
    }

    private void OnThermalVisionItemActionRemoved(Entity<ThermalVisionItemComponent> ent, ref ActionRemovedEvent args)
    {
        DisableThermalVisionItem(ent, ent.Comp.User);
    }

    private void OnThermalVisionItemRemove(Entity<ThermalVisionItemComponent> ent, ref ComponentRemove args)
    {
        DisableThermalVisionItem(ent, ent.Comp.User);
    }

    private void OnThermalVisionItemTerminating(Entity<ThermalVisionItemComponent> ent, ref EntityTerminatingEvent args)
    {
        DisableThermalVisionItem(ent, ent.Comp.User);
    }

    public void Toggle(Entity<ThermalVisionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.State = ent.Comp.State switch
        {
            ThermalVisionState.Off => ThermalVisionState.Full,
            ThermalVisionState.Full => ThermalVisionState.Off,
            _ => throw new ArgumentOutOfRangeException(),
        };

        Dirty(ent);
        UpdateAlert((ent, ent.Comp));
    }

    private void UpdateAlert(Entity<ThermalVisionComponent> ent)
    {
        ThermalVisionChanged(ent);
    }

    private void ToggleThermalVisionItem(Entity<ThermalVisionItemComponent> item, EntityUid user)
    {
        if (item.Comp.User == user && item.Comp.Toggleable)
        {
            DisableThermalVisionItem(item, item.Comp.User);
            return;
        }

        EnableThermalVisionItem(item, user);
    }

    private void EnableThermalVisionItem(Entity<ThermalVisionItemComponent> item, EntityUid user)
    {
        DisableThermalVisionItem(item, item.Comp.User);

        item.Comp.User = user;
        Dirty(item);

        _appearance.SetData(item, ThermalVisionItemVisuals.Active, true);

        if (!_timing.ApplyingState)
        {
            var nightVision = EnsureComp<ThermalVisionComponent>(user);
            if (TryComp<ThermalVisionComponent>(item, out var thermal))
            {
                nightVision.Color = thermal.Color;
            }
            nightVision.State = ThermalVisionState.Full;
            Dirty(user, nightVision);
        }

        _actions.SetToggled(item.Comp.Action, true);
    }

    protected virtual void ThermalVisionChanged(Entity<ThermalVisionComponent> ent)
    {
    }

    protected virtual void ThermalVisionRemoved(Entity<ThermalVisionComponent> ent)
    {
    }

    protected void DisableThermalVisionItem(Entity<ThermalVisionItemComponent> item, EntityUid? user)
    {
        _actions.SetToggled(item.Comp.Action, false);

        item.Comp.User = null;
        Dirty(item);

        _appearance.SetData(item, ThermalVisionItemVisuals.Active, false);

        if (TryComp(user, out ThermalVisionComponent? nightVision) &&
            !nightVision.Innate)
        {
            RemCompDeferred<ThermalVisionComponent>(user.Value);
        }
    }
}
