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

    public const float BaseBufferCapacity = 1500f;
    public const float BufferCapacityPerTier = 1500f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChemMasterComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<ChemMasterComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, ChemMasterComponent component, RefreshPartsEvent args)
    {
        var servoTier = args.GetPartRating(MachinePartIds.Servo, 1f);
        var capacity = BaseBufferCapacity + BufferCapacityPerTier * MathF.Max(0f, servoTier - 1f);

        if (TryComp<ChemMasterBufferComponent>(uid, out var buffer))
        {
            buffer.BufferCapacity = capacity;
            buffer.BaseBufferCapacity = BaseBufferCapacity;
        }

        if (_solution.TryGetSolution(uid, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
            bufferSolution.MaxVolume = capacity;
    }

    private void OnUpgradeExamine(EntityUid uid, ChemMasterComponent component, UpgradeExamineEvent args)
    {
        if (!TryComp<ChemMasterBufferComponent>(uid, out var buffer))
            return;

        args.AddPercentageUpgrade("machine-upgrade-chem-master-buffer", buffer.BufferCapacity / buffer.BaseBufferCapacity);
    }
}
