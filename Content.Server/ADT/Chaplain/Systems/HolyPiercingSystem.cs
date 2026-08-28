using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.ADT.HolyDamage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Heretic.EntitySystems.PathSpecific;

namespace Content.Server.ADT.Chaplain;

public sealed class HolyPiercingSystem : EntitySystem
{
    private const string HolyDamageType = "Holy";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolyAffectedComponent, DamageModifyEvent>(OnDamageModify,
            after: [typeof(ChampionStanceSystem), typeof(HolyDamageMultiplierSystem)]);
    }

    private void OnDamageModify(Entity<HolyAffectedComponent> ent, ref DamageModifyEvent args)
    {
        if (args.Origin is not { } origin || !HasComp<ChaplainComponent>(origin))
            return;

        if (!args.OriginalDamage.DamageDict.TryGetValue(HolyDamageType, out var pierced) || pierced <= FixedPoint2.Zero)
            return;

        if (TryComp<HolyDamageMultiplierComponent>(ent, out var multiplier))
            pierced *= multiplier.Multiplier;

        if (args.Damage.DamageDict.TryGetValue(HolyDamageType, out var current) && current >= pierced)
            return;

        args.Damage.DamageDict[HolyDamageType] = pierced;
    }
}
