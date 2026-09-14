using Content.Shared.Damage.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.Weapons.Melee.Backstab;
public sealed class BackstabDamageMultipilierSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BackstabDamageMultipilierComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(Entity<BackstabDamageMultipilierComponent> ent, ref MeleeHitEvent args)
    {
        foreach (var damaged in args.HitEntities)
        {
            if (damaged == args.User)
                continue;

            if (!IsBehindTarget(args.User, damaged))
                continue;

            _damageable.TryChangeDamage(damaged, ent.Comp.BonusDamage, origin: args.User);
        }
    }

    private bool IsBehindTarget(EntityUid user, EntityUid target)
    {
        var targetFacingDirection = _transform.GetWorldRotation(target).GetCardinalDir();
        var behindAngle = targetFacingDirection.GetOpposite().ToAngle();

        var userMapPos = _transform.GetMapCoordinates(user);
        var targetMapPos = _transform.GetMapCoordinates(target);
        var currentAngle = (userMapPos.Position - targetMapPos.Position).ToWorldAngle();

        var differenceFromBehindAngle = (behindAngle.Degrees - currentAngle.Degrees + 180 + 360) % 360 - 180;

        if (differenceFromBehindAngle > -45 && differenceFromBehindAngle < 45)
            return true;

        return false;
    }
}
