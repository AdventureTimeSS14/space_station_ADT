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
            if (args.EntityManager.TryGetComponent<MetaDataComponent>(item, out var meta) && meta.EntityPrototype?.ID == args.Listing.ProductAction)
                return false;
        }
        return true;
    }
}
