using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.ADT.Medical.IV;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Timing;
using System.Diagnostics.CodeAnalysis;

namespace Content.Server.ADT.Medical.IV;

public sealed class IVDripSystem : SharedIVDripSystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;

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
            !_solutionContainer.TryGetSolution(attachedTo, attachedStream.BloodSolutionName, out solEnt, out solution))
        {
            return false;
        }

        bloodstreamSolution = attachedStream.BloodSolution;
        return true;
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<IVDripComponent>();
        while (query.MoveNext(out var ivId, out var ivComp))
        {
            var attachedTo = ivComp.AttachedTo;
            if (attachedTo == EntityUid.Invalid)
                continue;

            if (!InRange((ivId, ivComp), ivComp.AttachedTo))
            {
                Logger.Warning($"[IV] Out of range -> detaching. IV={ivId} Target={ivComp.AttachedTo}");
                Detach((ivId, ivComp), true, false);
            }

            if (time < ivComp.TransferAt)
                continue;

            if (_itemSlots.GetItemOrNull(ivId, ivComp.Slot) is not { } pack)
                continue;

            if (!TryComp(pack, out BloodPackComponent? packComponent))
                continue;

            ivComp.TransferAt = time + ivComp.TransferDelay;

            if (!_solutionContainer.TryGetSolution(pack, packComponent.Solution, out var packSolEnt, out var packSol))
                continue;

            if (!TryGetBloodstream(attachedTo, out var streamSolEnt, out var streamSol, out var attachedStream))
                continue;

            if (ivComp.Injecting)
            {
                if (attachedStream is { } bloodSolutionEnt &&
                    bloodSolutionEnt.Comp.Solution.Volume < bloodSolutionEnt.Comp.Solution.MaxVolume)
                {
                    var beforePack = packSol.Volume;
                    var beforeBlood = bloodSolutionEnt.Comp.Solution.Volume;

                    // Don't transfer non-blood reagants
                    Solution excludedSolution = packSol.SplitSolutionWithout(packSol.MaxVolume, packComponent.TransferableReagents);
                    _solutionContainer.TryTransferSolution(bloodSolutionEnt, packSol, ivComp.TransferAmount);
                    _solutionContainer.TryAddSolution(packSolEnt.Value, excludedSolution);

                    var afterPack = packSol.Volume;
                    var afterBlood = bloodSolutionEnt.Comp.Solution.Volume;

                    Logger.Warning($"[IV TRANSFER -> PATIENT] IV={ivId} " +
                                   $"Pack: {beforePack:F2}->{afterPack:F2} " +
                                   $"Blood: {beforeBlood:F2}->{afterBlood:F2}");

                    Dirty(packSolEnt.Value);
                }
            }
            else
            {
                if (packSol.Volume < packSol.MaxVolume)
                {
                    var beforePack = packSol.Volume;
                    var beforeBlood = streamSol.Volume;

                    _solutionContainer.TryTransferSolution(packSolEnt.Value, streamSol, ivComp.TransferAmount);

                    var afterPack = packSol.Volume;
                    var afterBlood = streamSol.Volume;

                    Logger.Warning($"[IV TRANSFER <- PATIENT] IV={ivId} " +
                                   $"Pack: {beforePack:F2}->{afterPack:F2} " +
                                   $"Blood: {beforeBlood:F2}->{afterBlood:F2}");

                    Dirty(streamSolEnt.Value);
                }
            }

            ivComp.TransferAt = time + ivComp.TransferDelay;
            Dirty(ivId, ivComp);
        }
    }
}
