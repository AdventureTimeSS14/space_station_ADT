using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.Body.Events;
using Content.Shared.Buckle.Components;
using Content.Shared.Metabolism;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared.ADT.Medical.AdvancedMedBed;

public sealed class AdvancedMedBedSystem : EntitySystem
{
    [Dependency] private readonly MetabolizerSystem _metabolizer = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AdvancedMedBedComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<AdvancedMedBedComponent, UnstrappedEvent>(OnUnstrapped);
        SubscribeLocalEvent<AdvancedMedBedComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<AdvancedMedBedComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        SubscribeLocalEvent<AdvancedMedBedComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<AdvancedMedBedBuckledComponent, GetMetabolicMultiplierEvent>(OnGetMetabolicMultiplier);
    }

    private void OnStrapped(Entity<AdvancedMedBedComponent> bed, ref StrappedEvent args)
    {
        EnsureComp<AdvancedMedBedBuckledComponent>(args.Buckle);
        _metabolizer.UpdateMetabolicMultiplier(args.Buckle);
    }

    private void OnUnstrapped(Entity<AdvancedMedBedComponent> bed, ref UnstrappedEvent args)
    {
        RemComp<AdvancedMedBedBuckledComponent>(args.Buckle);
        _metabolizer.UpdateMetabolicMultiplier(args.Buckle);
    }

    private void OnRefreshParts(EntityUid uid, AdvancedMedBedComponent component, RefreshPartsEvent args)
    {
        var servoBonus = RefreshPartsEvent.GetTierMultiplier(args.GetPartRating(MachinePartIds.Servo, 1f), 0.05f) - 1f;
        var laserBonus = RefreshPartsEvent.GetTierMultiplier(args.GetPartRating(MachinePartIds.MicroLaser, 1f), 0.05f) - 1f;

        component.MetabolismMultiplier = 1f + servoBonus + laserBonus;
        UpdateMetabolisms(uid);
    }

    private void OnUpgradeExamine(EntityUid uid, AdvancedMedBedComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-metabolism-speed", component.MetabolismMultiplier, benefit: true);
    }

    private void OnPowerChanged(Entity<AdvancedMedBedComponent> ent, ref PowerChangedEvent args)
    {
        UpdateMetabolisms(ent.Owner);
    }

    private void OnGetMetabolicMultiplier(Entity<AdvancedMedBedBuckledComponent> ent, ref GetMetabolicMultiplierEvent args)
    {
        if (!TryComp<BuckleComponent>(ent, out var buckle) || buckle.BuckledTo is not { } bed)
            return;

        if (!TryComp<AdvancedMedBedComponent>(bed, out var component))
            return;

        if (!_powerReceiver.IsPowered(bed))
            return;

        args.Multiplier *= component.MetabolismMultiplier;
    }

    private void UpdateMetabolisms(Entity<AdvancedMedBedComponent?> bed)
    {
        if (!Resolve(bed, ref bed.Comp, false) || !TryComp<StrapComponent>(bed, out var strap))
            return;

        foreach (var buckled in strap.BuckledEntities)
            _metabolizer.UpdateMetabolicMultiplier(buckled);
    }
}