using Content.Server.Power.EntitySystems;
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.Body.Events;
using Content.Shared.Medical.Cryogenics;
using Content.Shared.Metabolism;
using Content.Shared.Power;
using Robust.Shared.Containers;

namespace Content.Server.ADT.Medical.CryoPod;
public sealed class CryoPodMetabolismSystem : EntitySystem
{
    [Dependency] private readonly MetabolizerSystem _metabolizer = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoPodMetabolismComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<CryoPodMetabolismComponent, UpgradeExamineEvent>(OnUpgradeExamine);
        SubscribeLocalEvent<CryoPodMetabolismComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<CryoPodMetabolismComponent, EntRemovedFromContainerMessage>(OnBodyRemoved);
        SubscribeLocalEvent<InsideCryoPodComponent, ComponentInit>(OnInsideCryoPodInit);
        SubscribeLocalEvent<InsideCryoPodComponent, GetMetabolicMultiplierEvent>(OnGetMetabolicMultiplier);
    }

    private void OnInsideCryoPodInit(Entity<InsideCryoPodComponent> ent, ref ComponentInit args)
    {
        _metabolizer.UpdateMetabolicMultiplier(ent.Owner);
    }

    private void OnPowerChanged(Entity<CryoPodMetabolismComponent> ent, ref PowerChangedEvent args)
    {
        if (TryComp<CryoPodComponent>(ent, out var cryoPod) && cryoPod.BodyContainer.ContainedEntity is { } patient)
            _metabolizer.UpdateMetabolicMultiplier(patient);
    }

    private void OnRefreshParts(EntityUid uid, CryoPodMetabolismComponent component, RefreshPartsEvent args)
    {
        var tier = Math.Min(
            args.GetPartRating(MachinePartIds.Capacitor, 1f),
            args.GetPartRating(MachinePartIds.ScanningModule, 1f));

        component.Tier = tier;
        component.Multiplier = tier switch
        {
            >= 5f => 1f / 1.8f,
            >= 4f => 1f / 1.4f,
            >= 3f => 1f / 1.25f,
            >= 2f => 1f / 1.1f,
            _ => 1f
        };

        if (TryComp<CryoPodComponent>(uid, out var cryoPod) && cryoPod.BodyContainer.ContainedEntity is { } patient)
            _metabolizer.UpdateMetabolicMultiplier(patient);
    }

    private void OnUpgradeExamine(EntityUid uid, CryoPodMetabolismComponent component, UpgradeExamineEvent args)
    {
        var speed = 1f / component.Multiplier;
        args.AddUpgradeLine(Loc.GetString("cryo-pod-metabolism-examine",
            ("speed", speed.ToString("0.##")),
            ("tier", component.Tier)));
    }

    private void OnBodyRemoved(Entity<CryoPodMetabolismComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != CryoPodComponent.BodyContainerName)
            return;

        _metabolizer.UpdateMetabolicMultiplier(args.Entity);
    }

    private void OnGetMetabolicMultiplier(Entity<InsideCryoPodComponent> ent, ref GetMetabolicMultiplierEvent args)
    {
        var pod = Transform(ent.Owner).ParentUid;
        if (!TryComp<CryoPodMetabolismComponent>(pod, out var comp) || !_power.IsPowered(pod))
            return;

        args.Multiplier *= comp.Multiplier;
    }
}
