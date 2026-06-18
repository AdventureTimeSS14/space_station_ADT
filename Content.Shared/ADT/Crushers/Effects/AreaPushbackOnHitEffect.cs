using Content.Shared.ADT.Crushers.Components;
using Content.Shared.ADT.Crawling;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Effects;
using Content.Shared.Mobs.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Marker;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Crushers.Effects;

[Serializable, NetSerializable]
public sealed partial class AreaPushbackOnHitEffect : TrophyEffect
{
    [DataField]
    public float Range = 2f;

    [DataField]
    public DamageSpecifier Damage = new DamageSpecifier()
    {
        DamageDict = { { "Heat", 5 } }
    };

    [DataField]
    public float PushbackForce = 200f;

    public override FormattedMessage GetDescription()
    {
        return FormattedMessage.FromMarkup(Loc.GetString("crusher-effect-area-pushback",
            ("range", Range.ToString("F1"))));
    }

    public override void OnMeleeHit(
        Entity<TrophyComponent> trophy,
        Entity<TrophyHolderComponent> holder,
        EntityManager entManager,
        ref MeleeHitEvent args)
    {
        foreach (var target in args.HitEntities)
        {
            if (target == holder.Owner)
                continue;

            if (!entManager.HasComponent<DamageMarkerComponent>(target))
                continue;

            DoAreaEffect(holder, entManager, args.User, target);
            break;
        }
    }

    private void DoAreaEffect(
        Entity<TrophyHolderComponent> holder,
        EntityManager entManager,
        EntityUid user,
        EntityUid directHit)
    {
        var damageableSystem = entManager.System<DamageableSystem>();
        var colorFlash = entManager.System<SharedColorFlashEffectSystem>();
        var transformSystem = entManager.System<SharedTransformSystem>();
        var physicsSystem = entManager.System<SharedPhysicsSystem>();
        var lookupSystem = entManager.System<EntityLookupSystem>();

        var userCoords = transformSystem.GetMapCoordinates(user);
        if (userCoords.MapId == Robust.Shared.Map.MapId.Nullspace)
            return;

        var entitiesInRange = lookupSystem.GetEntitiesInRange(userCoords, Range, LookupFlags.Uncontained);

        foreach (var ent in entitiesInRange)
        {
            if (ent == user || ent == holder.Owner)
                continue;

            if (!entManager.HasComponent<FaunaComponent>(ent))
                continue;

            var damaged = damageableSystem.TryChangeDamage(ent, Damage, origin: user);
            if (ent != directHit && damaged && !entManager.IsQueuedForDeletion(ent))
            {
                var filter = Filter.Pvs(ent, entityManager: entManager)
                    .RemoveWhereAttachedEntity(o => o == user);
                colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { ent }, filter);
            }

            if (!entManager.TryGetComponent<PhysicsComponent>(ent, out var physics))
                continue;

            if (physics.BodyType != BodyType.KinematicController)
                continue;

            var targetPos = transformSystem.GetWorldPosition(ent);
            var userPos = transformSystem.GetWorldPosition(user);
            var direction = targetPos - userPos;

            if (direction.LengthSquared() < 0.0001f)
                continue;

            var impulse = direction.Normalized() * PushbackForce;
            physicsSystem.ApplyLinearImpulse(ent, impulse, body: physics);
        }
    }
}
