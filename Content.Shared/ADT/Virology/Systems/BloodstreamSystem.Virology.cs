using System.Linq;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Shared.Body.Systems;

public sealed partial class BloodstreamSystem
{
    public void SetBloodDataVirology(Entity<BloodstreamComponent> ent, List<ReagentData> data)
    {
        ent.Comp.BloodData = data;
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.BloodData));

        ent.Comp.BloodReferenceSolution.SetReagentData(data.Select(d => d.Clone()).ToList());
        DirtyField(ent, ent.Comp, nameof(BloodstreamComponent.BloodReferenceSolution));

        if (!SolutionContainer.ResolveSolution(ent.Owner, ent.Comp.BloodSolutionName, ref ent.Comp.BloodSolution, out var solution))
            return;

        var bloodPrototypes = ent.Comp.BloodReferenceSolution.Contents
            .Select(r => r.Reagent.Prototype)
            .ToHashSet();

        var toReplace = solution.Contents
            .Where(r => bloodPrototypes.Contains(r.Reagent.Prototype))
            .ToList();

        if (toReplace.Count == 0)
            return;

        foreach (var reagent in toReplace)
        {
            solution.RemoveReagent(reagent);
        }

        foreach (var group in toReplace.GroupBy(r => r.Reagent.Prototype))
        {
            var quantity = FixedPoint2.Zero;
            foreach (var reagent in group)
            {
                quantity += reagent.Quantity;
            }

            solution.AddReagent(new ReagentId(group.Key, data.Select(d => d.Clone()).ToList()), quantity);
        }

        SolutionContainer.UpdateChemicals(ent.Comp.BloodSolution!.Value, needsReactionsProcessing: false);
    }
}
