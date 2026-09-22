using System.Linq;
using Content.Shared.ADT.Xenobiology;
using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Xenobiology.Systems;

/// <summary>
/// Handles the general behavior of slime extracts: reactions to reagents,
/// remaining uses and exhaustion.
/// </summary>
public sealed partial class SlimeExtractSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityEffectsSystem _entityEffectsSystem = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeExtractComponent, SolutionContainerChangedEvent>(OnSolutionChanged);
        SubscribeLocalEvent<SlimeExtractActiveReactionComponent, EntityPausedEvent>(OnPaused);
        SubscribeLocalEvent<SlimeExtractActiveReactionComponent, EntityUnpausedEvent>(OnUnpaused);
        SubscribeLocalEvent<SlimeExtractComponent, ExaminedEvent>(OnExamined);
    }

    private bool IsSolutionRequirementFulfilled(Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> requiredSolution, Solution currentSolution)
    {
        foreach (var req in requiredSolution)
        {
            var amount = currentSolution.GetTotalPrototypeQuantity(req.Key);
            if (amount < req.Value)
                return false;
        }

        return true;
    }

    private FixedPoint2 FindMinimumScalingFactor(Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> requiredSolution, Solution currentSolution)
    {
        var minimumScalingFactor = FixedPoint2.MaxValue;
        foreach (var req in requiredSolution)
        {
            var amount = currentSolution.GetTotalPrototypeQuantity(req.Key);
            minimumScalingFactor = FixedPoint2.Min(minimumScalingFactor, amount / req.Value);
        }
        return minimumScalingFactor;
    }

    private void OnSolutionChanged(Entity<SlimeExtractComponent> entity, ref SolutionContainerChangedEvent args)
    {
        if (_net.IsClient)
            return;

        if (TerminatingOrDeleted(entity.Owner))
            return;

        if (args.SolutionId != entity.Comp.ContainerName)
            return;

        EnsureComp<SlimeExtractActiveReactionComponent>(entity.Owner, out var activeReactionComponent);
        foreach (var extractReactionProto in entity.Comp.ExtractReactions)
        {
            var reaction = _prototypeManager.Index<ExtractReactionPrototype>(extractReactionProto);
            if (IsSolutionRequirementFulfilled(reaction.Requirements, args.Solution))
            {
                if (!activeReactionComponent.ActiveReactions.ContainsKey(extractReactionProto))
                    activeReactionComponent.ActiveReactions[extractReactionProto] = _gameTiming.CurTime + reaction.Delay;
            }
            else
                activeReactionComponent.ActiveReactions.Remove(extractReactionProto);
        }

        if (activeReactionComponent.ActiveReactions.Count == 0)
            RemComp<SlimeExtractActiveReactionComponent>(entity.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        if (_net.IsClient)
            return;

        var query = EntityQueryEnumerator<SlimeExtractComponent, SlimeExtractActiveReactionComponent>();
        while (query.MoveNext(out var uid, out var slimeExtractComponent, out var activeReactionComponent))
        {
            if (slimeExtractComponent.RemainingUses <= 0)
                continue;

            bool wasActivated = false;
            bool shouldDelete = false;
            List<ProtoId<ExtractReactionPrototype>>? completed = null;
            foreach (var reactionKey in activeReactionComponent.ActiveReactions.Keys.ToArray())
            {
                if (!activeReactionComponent.ActiveReactions.TryGetValue(reactionKey, out var deadline))
                    continue;

                if (deadline > _gameTiming.CurTime)
                    continue;

                if (!_solutionContainerSystem.TryGetSolution(uid, slimeExtractComponent.ContainerName, out _, out var currentSolution))
                    continue;

                var reaction = _prototypeManager.Index<ExtractReactionPrototype>(reactionKey);
                if (!IsSolutionRequirementFulfilled(reaction.Requirements, currentSolution))
                    continue;

                var minimumScalingFactor = FindMinimumScalingFactor(reaction.Requirements, currentSolution);
                foreach (var requirement in reaction.Requirements)
                {
                    var reagentToRemove = new ReagentQuantity(new ReagentId(requirement.Key, null),
                        minimumScalingFactor * requirement.Value);
                    currentSolution.RemoveReagent(reagentToRemove, false, true);
                }

                foreach (var effect in reaction.Effects)
                {
                    var factor = (minimumScalingFactor * effect.ScalingFactor) + effect.ScalingOffset;
                    _entityEffectsSystem.TryApplyEffect(uid, effect.Effect, factor.Float());
                }

                wasActivated = true;
                completed ??= new();
                completed.Add(reactionKey);
                if (reaction.ShouldDelete)
                    shouldDelete = true;
            }

            if (completed != null)
            {
                foreach (var completedReaction in completed)
                    activeReactionComponent.ActiveReactions.Remove(completedReaction);
            }

            if (wasActivated)
                slimeExtractComponent.RemainingUses -= 1;

            if (shouldDelete && slimeExtractComponent.RemainingUses <= 0)
                PredictedQueueDel(uid);
        }
    }

    private void OnPaused(Entity<SlimeExtractActiveReactionComponent> entity, ref EntityPausedEvent args)
    {
        if (_net.IsClient)
            return;

        entity.Comp.CurrentlyPaused = true;
    }

    private void OnUnpaused(Entity<SlimeExtractActiveReactionComponent> entity, ref EntityUnpausedEvent args)
    {
        if (_net.IsClient)
            return;

        entity.Comp.CurrentlyPaused = false;
        foreach (var activeReaction in entity.Comp.ActiveReactions.Keys)
        {
            entity.Comp.ActiveReactions[activeReaction] += args.PausedTime;
        }
    }

    private void OnExamined(Entity<SlimeExtractComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var str = ent.Comp.RemainingUses <= 0
            ? Loc.GetString("xeno-extract-exhausted")
            : Loc.GetString("xeno-extract-not-exhausted");

        args.PushMarkup(str);
    }
}
