using Content.Shared.Damage.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.Damage.Systems;

public sealed class DamageContactsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageContactsComponent, StartCollideEvent>(OnEntityEnter);
        SubscribeLocalEvent<DamageContactsComponent, EndCollideEvent>(OnEntityExit);
        SubscribeLocalEvent<DamageContactsComponent, ComponentShutdown>(OnDamageContactShutdown); // ADT-Tweak
        SubscribeLocalEvent<DamagedByContactComponent, MapInitEvent>(OnDamagedByContactMapInit); // ADT-Tweak
    }

    // ADT-Tweak start
    private void OnDamageContactShutdown(Entity<DamageContactsComponent> ent, ref ComponentShutdown args)
    {
        var query = EntityQueryEnumerator<DamagedByContactComponent>();
        while (query.MoveNext(out var damagedEnt, out var damaged))
        {
            damaged.Sources.Remove(ent.Owner);

            if (damaged.Sources.Count == 0)
            {
                RemComp<DamagedByContactComponent>(damagedEnt);
            }
        }
    }

    private void OnDamagedByContactMapInit(Entity<DamagedByContactComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Sources.RemoveWhere(s => !Exists(s));

        if (ent.Comp.Sources.Count == 0)
        {
            RemComp<DamagedByContactComponent>(ent);
        }
    }

    private bool HasActiveDamageContact(EntityUid damagedUid, EntityUid sourceUid)
    {
        if (!TryComp<PhysicsComponent>(damagedUid, out var body))
            return false;

        var contactingEntities = _physics.GetContactingEntities(damagedUid, body);
        return contactingEntities.Contains(sourceUid);
    }
    // ADT-Tweak end

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DamagedByContactComponent>();

        while (query.MoveNext(out var ent, out var damaged))
        {
            if (_timing.CurTime < damaged.NextSecond)
                continue;

            // ADT-Tweak start
            var hasActiveSource = false;
            foreach (var source in damaged.Sources)
            {
                if (Exists(source) && HasActiveDamageContact(ent, source))
                {
                    hasActiveSource = true;
                    break;
                }
            }

            if (!hasActiveSource)
            {
                RemComp<DamagedByContactComponent>(ent);
                continue;
            }
            // ADT-Tweak end

            damaged.NextSecond = _timing.CurTime + TimeSpan.FromSeconds(1);

            if (damaged.Damage != null)
                _damageable.TryChangeDamage(ent, damaged.Damage, interruptsDoAfters: false);
        }
    }

    private void OnEntityExit(EntityUid uid, DamageContactsComponent component, ref EndCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        if (!TryComp<DamagedByContactComponent>(otherUid, out var damagedByContact)) // ADT-Tweak
            return;

        // ADT-Tweak start
        damagedByContact.Sources.Remove(uid);

        if (damagedByContact.Sources.Count == 0)
        {
            RemComp<DamagedByContactComponent>(otherUid);
        }
        // ADT-Tweak end
    }

    private void OnEntityEnter(EntityUid uid, DamageContactsComponent component, ref StartCollideEvent args)
    {
        var otherUid = args.OtherEntity;

        if (_whitelistSystem.IsWhitelistPass(component.IgnoreWhitelist, otherUid))
            return;

        var damagedByContact = EnsureComp<DamagedByContactComponent>(otherUid);
        damagedByContact.Damage = component.Damage;
        damagedByContact.Sources.Add(uid); // ADT-Tweak
    }
}
