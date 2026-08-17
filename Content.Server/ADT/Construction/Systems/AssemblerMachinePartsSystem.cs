using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Events;

namespace Content.Server.ADT.Construction.Systems;

public sealed class AssemblerMachinePartsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AssemblerUpgradeComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<AssemblerUpgradeComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, AssemblerUpgradeComponent component, RefreshPartsEvent args)
    {
        component.IngredientMultiplier = args.GetStatMultiplier(MachineStat.ResourceCost);
    }

    private static void OnUpgradeExamine(EntityUid uid, AssemblerUpgradeComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-assembler-ingredients", component.IngredientMultiplier, benefit: false);
    }
}
