using Content.Server.Body.Systems;
using Content.Shared.ADT.Medical.IV;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.ADT.Medical.IV;

public sealed class IvDripSystem : SharedIvDripSystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _sharedSolutionContainer = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;

    private bool TryGetBloodstream(
        EntityUid attachedTo,
        [NotNullWhen(true)] out Entity<SolutionComponent>? solEnt,
        [NotNullWhen(true)] out Solution? solution,
        out Entity<SolutionComponent>? bloodstreamSolution)
    {
        solEnt = default;
        solution = default;
        bloodstreamSolution = default;
        if (!TryComp(attachedTo, out BloodstreamComponent? attachedStream) ||
            !_sharedSolutionContainer.TryGetSolution(attachedTo, attachedStream.BloodSolutionName, out solEnt, out solution))
            return false;

        bloodstreamSolution = attachedStream.BloodSolution;
        return true;
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<IVDripComponent>();
        while (query.MoveNext(out var ivId, out var ivComp))
        {
            if (ivComp.AttachedTo is not { } attachedTo)
                continue;

            if (!InRange((ivId, ivComp), attachedTo))
            {
                Detach((ivId, ivComp), true, false);
                continue;
            }

            if (time < ivComp.TransferAt)
                continue;

            if (_itemSlots.GetItemOrNull(ivId, ivComp.Slot) is not { } pack)
                continue;

            if (!TryComp(pack, out BloodPackComponent? packComponent))
                continue;

            ivComp.TransferAt = time + ivComp.TransferDelay;

            if (!_sharedSolutionContainer.TryGetSolution(pack, packComponent.Solution, out var packSolEnt, out var packSol))
                continue;

            if (!TryGetBloodstream(attachedTo, out var streamSolEnt, out var streamSol, out var attachedStream))
                continue;

            if (ivComp.Injecting)
            {
                if (attachedStream is { } bloodSolutionEnt &&
                    TryComp(attachedTo, out BloodstreamComponent? bloodstream))
                {
                    var beforePack = packSol.Volume;
                    var beforeBlood = bloodSolutionEnt.Comp.Solution.Volume;

                    var transferAmount = FixedPoint2.Min(ivComp.CurrentTransferAmount, packSol.Volume);
                    if (transferAmount > FixedPoint2.Zero)
                    {
                        var stepSolution = _sharedSolutionContainer.SplitSolution(packSolEnt.Value, transferAmount);
                        var nonBloodSolution = stepSolution.SplitSolutionWithout(
                            stepSolution.Volume,
                            packComponent.TransferableReagents.Select(r => new ProtoId<ReagentPrototype>(r)).ToArray()
                        );

                        var bloodOnlySolution = stepSolution;

                        if (bloodOnlySolution.Volume > FixedPoint2.Zero)
                        {
                            var addedBlood = _sharedSolutionContainer.AddSolution(bloodSolutionEnt, bloodOnlySolution);
                            var remainingBlood = bloodOnlySolution.Volume - addedBlood;
                            if (remainingBlood > FixedPoint2.Zero)
                            {
                                var overflow = bloodOnlySolution.Clone();
                                overflow.SplitSolution(addedBlood);
                                _sharedSolutionContainer.AddSolution(packSolEnt.Value, overflow);
                            }
                        }

                        if (nonBloodSolution.Volume > FixedPoint2.Zero)
                        {
                            if (!_bloodstream.TryAddToChemicals((attachedTo, bloodstream), nonBloodSolution))
                            {
                                _sharedSolutionContainer.AddSolution(packSolEnt.Value, nonBloodSolution);
                            }
                        }
                    }

                    var afterPack = packSol.Volume;
                    var afterBlood = bloodSolutionEnt.Comp.Solution.Volume;

                    Dirty(packSolEnt.Value);
                }
            }
            else
            {
                if (packSol.Volume < packSol.MaxVolume)
                {
                    var beforePack = packSol.Volume;
                    var beforeBlood = streamSol.Volume;

                    _sharedSolutionContainer.TryTransferSolution(packSolEnt.Value, streamSol, ivComp.CurrentTransferAmount);

                    var afterPack = packSol.Volume;
                    var afterBlood = streamSol.Volume;

                    Dirty(streamSolEnt.Value);
                }
            }

            ivComp.TransferAt = time + ivComp.TransferDelay;
            Dirty(ivId, ivComp);
        }
    }
}
