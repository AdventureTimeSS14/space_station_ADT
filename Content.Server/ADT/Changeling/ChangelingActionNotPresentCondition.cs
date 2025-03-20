using Content.Shared.Changeling.Components;
using Content.Shared.Store;

namespace Content.Server.Store.Conditions;

/// <summary>
/// Only allows a listing to be purchased while buyer can refresh.
/// </summary>
public sealed partial class ChangelingActionNotPresentCondition : ListingCondition
{
    public override bool Condition(ListingConditionArgs args)
    {
        if (!args.EntityManager.TryGetComponent<ChangelingComponent>(args.Buyer, out var ling))
            return false;

        foreach (var item in ling.BoughtActions)
        {
            if (ling.BoughtActions.TryGetValue(args.Listing.ProductAction ?? "", out _) || ling.BasicTransferredActions.TryGetValue(args.Listing.ProductAction ?? "", out _))
                return false;
        }
        return true;
    }
}
