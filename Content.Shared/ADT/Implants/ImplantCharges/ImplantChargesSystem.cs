using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Popups;
using Content.Shared.Trigger;

namespace Content.Shared.ADT.Implants.ImplantCharges;

public sealed class ImplantChargesSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSubdermalImplantSystem _implants = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplantChargesComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ImplantChargesComponent, ImplantImplantedEvent>(OnImplanted);
        SubscribeLocalEvent<ImplantChargesComponent, AttemptTriggerEvent>(OnAttemptTrigger);
        SubscribeLocalEvent<ImplantChargesComponent, TriggerEvent>(OnTrigger);
    }

    private void OnStartup(Entity<ImplantChargesComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Charges != null)
            return;

        ent.Comp.Charges = ent.Comp.MaxCharges;
        Dirty(ent);
    }

    private void OnImplanted(Entity<ImplantChargesComponent> ent, ref ImplantImplantedEvent args)
    {
        UpdateActionDescription(ent);
    }

    private void OnAttemptTrigger(Entity<ImplantChargesComponent> ent, ref AttemptTriggerEvent args)
    {
        if (!IsTracked(ent, args.Key) || ent.Comp.Charges > 0)
            return;

        args.Cancelled = true;

        if (ent.Comp.EmptyPopup is { } popup && args.User is { } user)
            _popup.PopupClient(Loc.GetString(popup), user, user, PopupType.SmallCaution);
    }

    private void OnTrigger(Entity<ImplantChargesComponent> ent, ref TriggerEvent args)
    {
        if (!IsTracked(ent, args.Key))
            return;

        var charges = Math.Max(0, (ent.Comp.Charges ?? ent.Comp.MaxCharges) - 1);
        ent.Comp.Charges = charges;
        Dirty(ent);

        UpdateActionDescription(ent);

        if (args.User is not { } user)
            return;

        if (ent.Comp.UsedPopup is { } popup)
            _popup.PopupClient(Loc.GetString(popup, ("charges", charges)), user, user);

        if (charges <= 0 && ent.Comp.RemoveWhenEmpty)
            _implants.ForceRemove(user, ent.Owner);
    }

    private static bool IsTracked(Entity<ImplantChargesComponent> ent, string? key)
    {
        return key == null || ent.Comp.Keys.Contains(key);
    }

    private void UpdateActionDescription(Entity<ImplantChargesComponent> ent)
    {
        if (ent.Comp.ActionDescription is not { } desc
            || !TryComp<SubdermalImplantComponent>(ent, out var implant)
            || implant.Action is not { } action)
        {
            return;
        }

        _metaData.SetEntityDescription(action, Loc.GetString(desc, ("charges", ent.Comp.Charges ?? ent.Comp.MaxCharges)));
    }
}
