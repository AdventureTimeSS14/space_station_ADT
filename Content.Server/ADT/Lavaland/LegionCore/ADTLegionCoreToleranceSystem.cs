using Content.Shared.ADT.Lavaland.LegionCore;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Lavaland.LegionCore;

public sealed class ADTLegionCoreToleranceSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public static readonly ProtoId<DamageTypePrototype> CellularDamage = "Cellular";

    public FixedPoint2 TakeCellularCost(EntityUid target, float multiplier)
    {
        var tolerance = EnsureComp<ADTLegionCoreToleranceComponent>(target);
        var cost = tolerance.CellularBase + tolerance.CellularStep * tolerance.Uses;

        if (cost > tolerance.CellularMax)
            cost = tolerance.CellularMax;

        cost *= multiplier;
        tolerance.Uses++;

        if (cost <= FixedPoint2.Zero)
            return FixedPoint2.Zero;

        var message = "adt-legion-core-cellular-cost";

        if (tolerance.Uses >= tolerance.WarningUses)
            message = "adt-legion-core-cellular-reject";

        _popup.PopupEntity(Loc.GetString(message), target, target, PopupType.MediumCaution);

        return cost;
    }
}
