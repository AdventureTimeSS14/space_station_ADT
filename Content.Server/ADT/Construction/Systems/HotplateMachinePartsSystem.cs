using Content.Server.Chemistry.Components;
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;

namespace Content.Server.ADT.Construction.Systems;

public sealed class HotplateMachinePartsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionHeaterComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<SolutionHeaterComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, SolutionHeaterComponent component, RefreshPartsEvent args)
    {
        component.HeatMultiplier = args.GetStatMultiplier(MachineStat.Speed);
    }

    private static void OnUpgradeExamine(EntityUid uid, SolutionHeaterComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-heat-power", component.HeatMultiplier, benefit: true);
    }
}
