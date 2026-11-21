//

using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Heretic.Components.PathSpecific.Blade;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;

namespace Content.Shared.ADT.Heretic.Systems.PathSpecific.Blade;

public abstract partial class SharedSacramentsSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _dmg = default!;
    [Dependency] private readonly SharedStaminaSystem _stam = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityQuery<SacramentsOfPowerComponent> _sacramentsQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobStateComponent, DamageDealtEvent>(OnDamage);
        SubscribeLocalEvent<SacramentsOfPowerComponent, BeforeStaminaDamageEvent>(OnBeforeStamina);
        SubscribeLocalEvent<SacramentsOfPowerComponent, BeforeDamageChangedEvent>(OnBeforeDamageChange);
    }

    private void OnDamage(Entity<MobStateComponent> ent, ref DamageDealtEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        AddIgnoredEntity(ent, args.Origin);
    }

    private void AddIgnoredEntity(EntityUid uid, EntityUid? origin)
    {
        if (origin is not { } || !_sacramentsQuery.TryComp(origin.Value, out var comp))
            return;

        if (!comp.IgnoredEntities.Add(uid))
            return;

        var heretic = Identity.Entity(origin.Value, EntityManager, uid);
        _popup.PopupEntity(Loc.GetString("heretic-sacraments-can-attack", ("heretic", heretic)), uid, uid, PopupType.Medium);
        Dirty(origin.Value, comp);
    }

    public bool ShouldBlockDamage(Entity<SacramentsOfPowerComponent?> ent, EntityUid? user)
    {
        return ent != user && _sacramentsQuery.Resolve(ent, ref ent.Comp, false) &&
            ent.Comp.State == SacramentsState.Open &&
            (user is not { } || !ent.Comp.IgnoredEntities.Contains(user.Value));
    }

    private void OnBeforeStamina(Entity<SacramentsOfPowerComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        if (args.Value <= 0)
            return;

        if (!ShouldBlockDamage(ent.AsNullable(), null))
            return;

        args.Cancelled = true;
        Pulse(ent);
    }

    private void OnBeforeDamageChange(Entity<SacramentsOfPowerComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (!args.Damage.AnyPositive())
            return;

        if (!ShouldBlockDamage(ent.AsNullable(), args.Origin))
            return;

        args.Cancelled = true;
        Pulse(ent);

        if (args.Origin is not { } origin)
            return;

        _dmg.TryChangeDamage(origin, args.Damage * ent.Comp.DamageReturnRatio, origin: ent);
    }

    protected virtual void Pulse(EntityUid ent) { }
}
