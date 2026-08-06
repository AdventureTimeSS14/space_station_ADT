using Content.Shared.EntityConditions;
using Content.Shared.Whitelist;
using Content.Shared.ADT.Areas;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityConditions;

/// <summary>
/// Checks that the target entity is in an area, and optionally matches a whitelist.
/// </summary>
public sealed partial class InsideAreaCondition : EntityConditionBase<InsideAreaCondition>
{
    /// <summary>
    /// A whitelist the area must match if non-null.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// A blacklist the area cannot match if non-null.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Guidebook text explaining the area condition.
    /// </summary>
    [DataField]
    public LocId GuidebookText = "entity-condition-guidebook-inside-area";

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
        => Loc.GetString(GuidebookText);
}

public sealed class InsideAreaConditionSystem : EntityConditionSystem<TransformComponent, InsideAreaCondition>
{
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    protected override void Condition(Entity<TransformComponent> ent, ref EntityConditionEvent<InsideAreaCondition> args)
    {
        args.Result = CheckArea(ent.Comp.Coordinates, args.Condition);
    }

    public bool CheckArea(EntityCoordinates coords, InsideAreaCondition cond)
    {
        if (_area.GetArea(coords) is not {} area)
            return false;

        return _whitelist.CheckBoth(area, blacklist: cond.Blacklist, whitelist: cond.Whitelist);
    }
}