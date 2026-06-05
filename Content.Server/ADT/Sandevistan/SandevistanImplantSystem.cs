using Content.Shared.ADT.Sandevistan;
using Content.Shared.GrabProtection;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Popups;

namespace Content.Server.ADT.Sandevistan;

public sealed class SandevistanImplantSystem : EntitySystem
{
    [Dependency] private readonly SharedSubdermalImplantSystem _implants = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    // коммент до почина
    // [UISystemDependency] private readonly VisualBodySystem _visualBody = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SandevistanImplantComponent, ImplantImplantedEvent>(OnImplanted);
        SubscribeLocalEvent<SandevistanImplantComponent, ImplantRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<StatvekaSandevistanImplantComponent, ImplantImplantedEvent>(OnStatvekaImplanted);
        SubscribeLocalEvent<StatvekaSandevistanImplantComponent, ImplantRemovedEvent>(OnStatvekaRemoved);
    }

    private bool HasConflictingImplant<T>(EntityUid uid, EntityUid implanted) where T : Component
    {
        if (!TryComp<ImplantedComponent>(implanted, out var implantedComp))
            return false;

        foreach (var existing in implantedComp.ImplantContainer.ContainedEntities)
        {
            if (existing == uid)
                continue;

            if (HasComp<T>(existing))
                return true;
        }

        return false;
    }

    private void OnImplanted(EntityUid uid, SandevistanImplantComponent comp, ref ImplantImplantedEvent args)
    {
        var owner = args.Implanted;

        if (HasConflictingImplant<StatvekaSandevistanImplantComponent>(uid, owner))
        {
            _popup.PopupEntity(Loc.GetString("sandevistan-implant-conflict"), owner, owner, PopupType.LargeCaution);
            _implants.ForceRemove(owner, uid);
            return;
        }

        EnsureComp<SandevistanUserComponent>(owner);
        EnsureComp<GrabProtectionComponent>(owner);

        // коммент до почина
        // if (!string.IsNullOrEmpty(comp.MarkingId) && TryComp<HumanoidProfileComponent>(owner, out var visualOrganMarkingsComponent))
        // {
        //     _visualBody.AddMarking(owner, comp.MarkingId, comp.MarkingColor, sync: true, forced: comp.ForcedMarking);
        // }
    }

    private void OnRemoved(EntityUid uid, SandevistanImplantComponent comp, ref ImplantRemovedEvent args)
    {
        var owner = args.Implanted;

        if (TryComp<SandevistanUserComponent>(owner, out _))
        {
            RemComp<SandevistanUserComponent>(owner);
            RemComp<GrabProtectionComponent>(owner);

            // коммент до почина
            // if (!string.IsNullOrEmpty(comp.MarkingId) && TryComp<HumanoidProfileComponent>(owner, out var visualOrganMarkingsComponent))
            // {
            //     _visualBody.RemoveMarking(owner, comp.MarkingId, sync: true);
            // }
        }
    }

    private void OnStatvekaImplanted(EntityUid uid, StatvekaSandevistanImplantComponent comp, ref ImplantImplantedEvent args)
    {
        var owner = args.Implanted;

        if (HasConflictingImplant<SandevistanImplantComponent>(uid, owner))
        {
            _popup.PopupEntity(Loc.GetString("sandevistan-implant-conflict"), owner, owner, PopupType.LargeCaution);
            _implants.ForceRemove(owner, uid);
            return;
        }

        var user = EnsureComp<SandevistanUserComponent>(owner);
        user.MovementSpeedModifier = comp.MovementSpeedModifier;
        user.AttackSpeedModifier = comp.AttackSpeedModifier;
        user.DoAfterSpeedModifier = comp.DoAfterSpeedModifier;
        user.SlowfieldEnabled = comp.SlowfieldEnabled;
        user.SlowfieldRadius = comp.SlowfieldRadius;
        user.SlowfieldMobMultiplier = comp.SlowfieldMobMultiplier;
        user.Thresholds = comp.Thresholds;

        EnsureComp<GrabProtectionComponent>(owner);
    }

    private void OnStatvekaRemoved(EntityUid uid, StatvekaSandevistanImplantComponent comp, ref ImplantRemovedEvent args)
    {
        RemComp<SandevistanUserComponent>(args.Implanted);
        RemComp<GrabProtectionComponent>(args.Implanted);
    }
}
