using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Heretic;
using Content.Shared.Store;

namespace Content.Server.Store.Conditions;

// ADT: moved from Content.Server._Shitcode.Store.Conditions
public sealed partial class HereticPathCondition : ListingCondition
{
    [DataField]
    public HashSet<string>? Whitelist;

    [DataField]
    public HashSet<string>? Blacklist;

    [DataField]
    public int Stage;

    [DataField]
    public bool RequiresCanAscend;

    public override bool Condition(ListingConditionArgs args)
    {
        var ent = args.EntityManager;
        var hereticSys = ent.System<SharedHereticSystem>();

        if (!hereticSys.TryGetHereticComponent(args.Buyer, out var hereticComp, out _) &&
            !ent.TryGetComponent(args.Buyer, out hereticComp))
            return false;

        if (RequiresCanAscend && !hereticComp.CanAscend)
            return false;

        if (Stage > hereticComp.PathStage)
            return false;

        if (Whitelist != null)
        {
            foreach (var white in Whitelist)
                if (hereticComp.CurrentPath == white)
                    return true;
            return false;
        }

        if (Blacklist != null)
        {
            foreach (var black in Blacklist)
                if (hereticComp.CurrentPath == black)
                    return false;
            return true;
        }

        return true;
    }
}
