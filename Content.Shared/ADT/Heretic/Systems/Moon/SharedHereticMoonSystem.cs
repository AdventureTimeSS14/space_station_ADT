using Content.Shared.ADT.Heretic.Components.Moon;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Heretic.Systems.Moon;

public abstract class SharedHereticMoonSystem : EntitySystem
{
    [Dependency] protected readonly DamageableSystem Damage = default!;
    [Dependency] protected readonly IPrototypeManager Proto = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;

    private DamageTypePrototype _cellular = default!;
    private DamageTypePrototype _heat = default!;

    public override void Initialize()
    {
        base.Initialize();
        _cellular = Proto.Index<DamageTypePrototype>("Cellular");
        _heat = Proto.Index<DamageTypePrototype>("Heat");
    }

    public void EnsureSanity(EntityUid uid)
    {
        EnsureComp<HereticSanityComponent>(uid);
    }

    public float GetSanity(EntityUid uid)
    {
        if (!TryComp<HereticSanityComponent>(uid, out var sanity))
            return 150f;
        return sanity.Current;
    }

    public void AdjustSanity(EntityUid uid, float amount)
    {
        var comp = EnsureComp<HereticSanityComponent>(uid);
        comp.Current = Math.Clamp(comp.Current + amount, 0f, comp.Max);
        Dirty(uid, comp);
    }

    public void DealCellular(EntityUid target, float amount, EntityUid? origin = null)
    {
        Damage.TryChangeDamage(target, new DamageSpecifier(_cellular, amount), origin: origin);
    }

    public void DealHeat(EntityUid target, float amount, EntityUid? origin = null)
    {
        Damage.TryChangeDamage(target, new DamageSpecifier(_heat, amount), origin: origin);
    }

    public bool IsHereticOrLunatic(EntityUid uid)
    {
        if (HasComp<MoonLunaticComponent>(uid))
            return true;
        return _heretic.IsHereticOrGhoul(uid);
    }
}
