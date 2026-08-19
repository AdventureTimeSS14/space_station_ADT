using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.Xenoarchaeology.Equipment.Components;

namespace Content.Server.ADT.Construction.Systems;

public sealed class ArtifactCrusherMachinePartsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArtifactCrusherComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<ArtifactCrusherComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, ArtifactCrusherComponent component, RefreshPartsEvent args)
    {
        if (!TryComp<ArtifactCrusherUpgradeComponent>(uid, out var upgrade))
            return;

        var speed = args.GetStatMultiplier(MachineStat.Speed);

        upgrade.WorkTimeMultiplier = 1f / speed;
        upgrade.OutputMultiplier = args.GetStatMultiplier(MachineStat.Output);
    }

    private void OnUpgradeExamine(EntityUid uid, ArtifactCrusherComponent component, UpgradeExamineEvent args)
    {
        if (!TryComp<ArtifactCrusherUpgradeComponent>(uid, out var upgrade))
            return;

        args.AddPercentageUpgrade("machine-upgrade-process-speed", 1f / upgrade.WorkTimeMultiplier, benefit: true);
        args.AddPercentageUpgrade("machine-upgrade-fragment-output", upgrade.OutputMultiplier, benefit: true);
    }
}
