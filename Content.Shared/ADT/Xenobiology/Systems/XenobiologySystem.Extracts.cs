using Content.Shared.Chemistry;
using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.Chemistry.Reaction;
using Content.Shared.Examine;

namespace Content.Shared.ADT.Xenobiology.Systems;

/// <summary>
/// This handles slime extracts.
/// </summary>
public partial class XenobiologySystem
{
    private void InitializeExtracts()
    {
        SubscribeLocalEvent<SlimeExtractComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<SlimeExtractComponent, BeforeSolutionReactEvent>(BeforeSolutionReact);
    }

    private void OnExamined(Entity<SlimeExtractComponent> extract, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange
            || !TryComp<ReactiveComponent>(extract, out var reactive))
            return;

        var message = Loc.GetString("slime-extract-examined-charges", ("num", reactive.RemainingReactions));
        if (reactive.IsReactionsUnlimited)
            message = Loc.GetString("slime-extract-examined-charges-infinite");

        args.PushMarkup(message);
    }
    private void BeforeSolutionReact(Entity<SlimeExtractComponent> extract, ref BeforeSolutionReactEvent args)
    {
        // clean up the reagents inside when performing an effect
        if (_solution.TryGetRefillableSolution(extract.Owner, out var soln, out _))
            _solution.RemoveAllSolution((extract.Owner, soln));
    }
}
