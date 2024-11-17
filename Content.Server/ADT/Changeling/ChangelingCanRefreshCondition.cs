using Content.Shared.Changeling.Components;
using Content.Shared.Store;

namespace Content.Server.Store.Conditions;

/// <summary>
/// Only allows a listing to be purchased while buyer can refresh.
/// </summary>
public sealed partial class ChangelingCanRefreshCondition : ListingCondition
{

    public override bool Condition(ListingConditionArgs args)
    {
        return args.EntityManager.TryGetComponent<ChangelingComponent>(args.Buyer, out var ling) && ling.CanRefresh;
    }
}
