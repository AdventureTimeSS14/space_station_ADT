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
        [NotNullWhen(true)] out Solution? solution)
    {
        solEnt = default;
        solution = default;

        if (!TryComp(attachedTo, out BloodstreamComponent? attachedStream))
            return false;

        return _sharedSolutionContainer.TryGetSolution(attachedTo, attachedStream.BloodSolutionName, out solEnt, out solution);
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

            if (!_sharedSolutionContainer.TryGetSolution(pack, packComponent.Solution, out var packSolEnt, out var packSol))
                continue;

            if (!TryGetBloodstream(attachedTo, out var streamSolEnt, out var streamSol))
                continue;

            ivComp.TransferAt = time + ivComp.TransferDelay;

            if (ivComp.Injecting)
            {
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
                        var addedBlood = _sharedSolutionContainer.AddSolution(streamSolEnt.Value, bloodOnlySolution);
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
                        if (!_bloodstream.TryAddToBloodstream(attachedTo, nonBloodSolution))
                        {
                            _sharedSolutionContainer.AddSolution(packSolEnt.Value, nonBloodSolution);
                        }
                    }
                }

                Dirty(packSolEnt.Value);
                Dirty(streamSolEnt.Value);
            }
            else
            {
                if (packSol.Volume < packSol.MaxVolume)
                {
                    _sharedSolutionContainer.TryTransferSolution(packSolEnt.Value, streamSol, ivComp.CurrentTransferAmount);
                    Dirty(streamSolEnt.Value);
                    Dirty(packSolEnt.Value);
                }
            }

            Dirty(ivId, ivComp);
        }
    }
}
