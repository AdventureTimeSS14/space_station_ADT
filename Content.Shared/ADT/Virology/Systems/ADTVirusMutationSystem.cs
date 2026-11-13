using System.Linq;
using Content.Shared.ADT.Virology.Effects;
using Content.Shared.Body;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reaction;

namespace Content.Shared.ADT.Virology.Systems;

public sealed class ADTVirusMutationSystem : EntitySystem
{
    [Dependency] private SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionComponent, ReactionAttemptEvent>(OnReactionAttempt);
    }

    private void OnReactionAttempt(Entity<SolutionComponent> ent, ref ReactionAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var mix = false;
        var mutation = false;
        foreach (var effect in args.Reaction.Effects)
        {
            if (effect is ADTMixViruses)
                mix = true;
            else if (effect is ADTEvolveVirus or ADTDevolveVirus)
                mutation = true;
        }

        if (!mix && !mutation)
            return;

        var owner = _solution.GetSolutionOwner(ent);
        if (HasComp<BodyComponent>(owner) || HasComp<OrganComponent>(owner))
        {
            args.Cancelled = true;
            return;
        }

        if (mix && !NeedsMix(ent.Comp.Solution))
            args.Cancelled = true;
    }

    public static ADTVirusData? GetVirusData(ReagentQuantity reagent)
    {
        return reagent.Reagent.Data?.OfType<ADTVirusData>().FirstOrDefault();
    }

    public static bool NeedsMix(Solution solution)
    {
        string? firstId = null;
        foreach (var reagent in solution.Contents)
        {
            var data = GetVirusData(reagent);
            if (data == null)
                continue;

            foreach (var disease in data.Diseases)
            {
                if (!disease.IsAdvance)
                    continue;

                var id = disease.GetId();
                if (firstId == null)
                    firstId = id;
                else if (firstId != id)
                    return true;
            }
        }

        return false;
    }
}
