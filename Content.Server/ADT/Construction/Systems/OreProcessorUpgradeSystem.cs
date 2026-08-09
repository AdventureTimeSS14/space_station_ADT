using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Events;

namespace Content.Server.ADT.Construction.Systems;

public sealed class OreProcessorUpgradeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OreProcessorUpgradeComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<OreProcessorUpgradeComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, OreProcessorUpgradeComponent component, RefreshPartsEvent args)
    {
        component.OutputMultiplier = args.GetPartRating(component.OutputPart, 1f);
        component.PointsMultiplier = args.GetPartRating(component.PointsPart, 1f);
    }

    private static void OnUpgradeExamine(EntityUid uid, OreProcessorUpgradeComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-ore-output", component.OutputMultiplier);
        args.AddPercentageUpgrade("machine-upgrade-ore-points", component.PointsMultiplier);
    }
}
