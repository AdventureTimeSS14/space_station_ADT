using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Inventory.Events;
using Content.Shared.Rounding;
using Content.Shared.Toggleable;
using Robust.Shared.Timing;
using Content.Shared.ADT.Eye.Blinding;

namespace Content.Shared.ADT.MesonVision;

public abstract class SharedMesonVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MesonVisionComponent, ComponentStartup>(OnMesonVisionStartup);
        SubscribeLocalEvent<MesonVisionComponent, MapInitEvent>(OnMesonVisionMapInit);
        SubscribeLocalEvent<MesonVisionComponent, AfterAutoHandleStateEvent>(OnMesonVisionAfterHandle);
        SubscribeLocalEvent<MesonVisionComponent, ComponentRemove>(OnMesonVisionRemove);

        SubscribeLocalEvent<MesonVisionItemComponent, GetItemActionsEvent>(OnMesonVisionItemGetActions);
        SubscribeLocalEvent<MesonVisionItemComponent, ToggleActionEvent>(OnMesonVisionItemToggle);
        SubscribeLocalEvent<MesonVisionItemComponent, GotEquippedEvent>(OnMesonVisionItemGotEquipped);
        SubscribeLocalEvent<MesonVisionItemComponent, GotUnequippedEvent>(OnMesonVisionItemGotUnequipped);
        SubscribeLocalEvent<MesonVisionItemComponent, ActionRemovedEvent>(OnMesonVisionItemActionRemoved);
        SubscribeLocalEvent<MesonVisionItemComponent, ComponentRemove>(OnMesonVisionItemRemove);
        SubscribeLocalEvent<MesonVisionItemComponent, EntityTerminatingEvent>(OnMesonVisionItemTerminating);
    }

    private void OnMesonVisionStartup(Entity<MesonVisionComponent> ent, ref ComponentStartup args)
    {
        MesonVisionChanged(ent);
    }

    private void OnMesonVisionAfterHandle(Entity<MesonVisionComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        MesonVisionChanged(ent);
    }

    private void OnMesonVisionMapInit(Entity<MesonVisionComponent> ent, ref MapInitEvent args)
    {
        UpdateAlert(ent);
    }

    private void OnMesonVisionRemove(Entity<MesonVisionComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.Alert is { } alert)
            _alerts.ClearAlert(ent, alert);

        MesonVisionRemoved(ent);
    }

    private void OnMesonVisionItemGetActions(Entity<MesonVisionItemComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.InHands || !ent.Comp.Toggleable)
            return;

        if (ent.Comp.SlotFlags != args.SlotFlags)
            return;

        args.AddAction(ref ent.Comp.Action, ent.Comp.ActionId);
    }

    private void OnMesonVisionItemToggle(Entity<MesonVisionItemComponent> ent, ref ToggleActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ToggleMesonVisionItem(ent, args.Performer);
    }

    private void OnMesonVisionItemGotEquipped(Entity<MesonVisionItemComponent> ent, ref GotEquippedEvent args)
    {
        if (ent.Comp.SlotFlags != args.SlotFlags)
            return;

        EnableMesonVisionItem(ent, args.Equipee);
    }

    private void OnMesonVisionItemGotUnequipped(Entity<MesonVisionItemComponent> ent, ref GotUnequippedEvent args)
    {
        if (ent.Comp.SlotFlags != args.SlotFlags)
            return;

        DisableMesonVisionItem(ent, args.Equipee);
    }

    private void OnMesonVisionItemActionRemoved(Entity<MesonVisionItemComponent> ent, ref ActionRemovedEvent args)
    {
        DisableMesonVisionItem(ent, ent.Comp.User);
    }

    private void OnMesonVisionItemRemove(Entity<MesonVisionItemComponent> ent, ref ComponentRemove args)
    {
        DisableMesonVisionItem(ent, ent.Comp.User);
    }

    private void OnMesonVisionItemTerminating(Entity<MesonVisionItemComponent> ent, ref EntityTerminatingEvent args)
    {
        DisableMesonVisionItem(ent, ent.Comp.User);
    }

    public void Toggle(Entity<MesonVisionComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        ent.Comp.State = ent.Comp.State switch
        {
            MesonVisionState.Off => MesonVisionState.Full,
            MesonVisionState.Full => MesonVisionState.Off,
            _ => throw new ArgumentOutOfRangeException(),
        };

        Dirty(ent);
        UpdateAlert((ent, ent.Comp));
    }

    private void UpdateAlert(Entity<MesonVisionComponent> ent)
    {
        if (ent.Comp.Alert is { } alert)
        {
            var level = MathF.Max((int)MesonVisionState.Off, (int)ent.Comp.State);
            var max = _alerts.GetMaxSeverity(alert);
            var severity = max - ContentHelpers.RoundToLevels(level, (int)MesonVisionState.Full, max + 1);
            _alerts.ShowAlert(ent, alert, (short)severity);
        }

        MesonVisionChanged(ent);
    }

    private void ToggleMesonVisionItem(Entity<MesonVisionItemComponent> item, EntityUid user)
    {
        if (item.Comp.User == user && item.Comp.Toggleable)
        {
            DisableMesonVisionItem(item, item.Comp.User);
            return;
        }

        EnableMesonVisionItem(item, user);
    }

    private void EnableMesonVisionItem(Entity<MesonVisionItemComponent> item, EntityUid user)
    {
        DisableMesonVisionItem(item, item.Comp.User);

        item.Comp.User = user;
        Dirty(item);

        _appearance.SetData(item, MesonVisionItemVisuals.Active, true);

        if (!_timing.ApplyingState)
        {
            var nightVision = EnsureComp<MesonVisionComponent>(user);
            nightVision.State = MesonVisionState.Full;
            Dirty(user, nightVision);

            var eyeDamage = EnsureComp<DamageEyesOnFlashedComponent>(user);
            Dirty(user, eyeDamage);
        }

        _actions.SetToggled(item.Comp.Action, true);
    }

    protected virtual void MesonVisionChanged(Entity<MesonVisionComponent> ent)
    {
    }

    protected virtual void MesonVisionRemoved(Entity<MesonVisionComponent> ent)
    {
    }

    protected void DisableMesonVisionItem(Entity<MesonVisionItemComponent> item, EntityUid? user)
    {
        _actions.SetToggled(item.Comp.Action, false);

        item.Comp.User = null;
        Dirty(item);

        _appearance.SetData(item, MesonVisionItemVisuals.Active, false);

        if (TryComp(user, out MesonVisionComponent? nightVision) &&
            !nightVision.Innate)
        {
            RemCompDeferred<MesonVisionComponent>(user.Value);
            RemCompDeferred<DamageEyesOnFlashedComponent>(user.Value);
        }
    }
}
