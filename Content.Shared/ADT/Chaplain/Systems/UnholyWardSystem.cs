using Content.Shared.ADT.Armor;
using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.ADT.HolyDamage;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.Projectiles;

namespace Content.Shared.ADT.Chaplain.Systems;

public sealed class UnholyWardSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnholyWardComponent, InventoryRelayedEvent<DamageModifyEvent>>(OnDamageModify);
        SubscribeLocalEvent<UnholyWardComponent, ExaminedEvent>(OnExamined);
    }

    private void OnDamageModify(Entity<UnholyWardComponent> ent, ref InventoryRelayedEvent<DamageModifyEvent> args)
    {
        if (!IsUnholy(args.Args.Origin))
            return;

        var ev = new ArmorProtectAttemptEvent(args.Owner);
        RaiseLocalEvent(ent, ref ev);
        if (ev.Cancelled)
            return;

        args.Args.Damage = DamageSpecifier.ApplyModifierSet(args.Args.Damage, ent.Comp.Modifiers);
    }

    private void OnExamined(Entity<UnholyWardComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("unholy-ward-examine"));
    }

    public bool IsUnholy(EntityUid? origin)
    {
        if (origin is not { } uid)
            return false;

        if (HasComp<HolyAffectedComponent>(uid))
            return true;

        return TryComp<ProjectileComponent>(uid, out var projectile) && HasComp<HolyAffectedComponent>(projectile.Shooter);
    }
}
