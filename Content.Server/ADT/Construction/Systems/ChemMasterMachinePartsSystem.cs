using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.Chemistry;
using Content.Server.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;

namespace Content.Server.ADT.Construction.Systems;
public sealed class ChemMasterMachinePartsSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChemMasterComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<ChemMasterComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, ChemMasterComponent component, RefreshPartsEvent args)
    {
        if (!TryComp<ChemMasterBufferComponent>(uid, out var buffer))
            return;

        var binTier = args.GetPartRating(MachinePartIds.MatterBin, 1f);
        var capacity = MathF.Max(buffer.BufferCapacity, buffer.BufferCapacityPerTier * (binTier - 1f));
        buffer.BufferMultiplier = capacity / buffer.BufferCapacity;

        if (_solution.TryGetSolution(uid, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
            bufferSolution.MaxVolume = capacity;
    }

    private void OnUpgradeExamine(EntityUid uid, ChemMasterComponent component, UpgradeExamineEvent args)
    {
        if (!TryComp<ChemMasterBufferComponent>(uid, out var buffer))
            return;

        args.AddPercentageUpgrade("machine-upgrade-chem-master-buffer", buffer.BufferMultiplier, benefit: true);
    }
}
