using Content.Shared.ADT.TimeDespawnDamage;
using Content.Shared.Projectiles;
using Content.Shared.ADT.ProjectileDespawn;

public sealed class ProjectileDespawnSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ProjectileDespawnComponent, ProjectileHitEvent>(OnProjectileHits);
    }

    private void OnProjectileHits(EntityUid uid, ProjectileDespawnComponent component, ProjectileHitEvent args)
    {
        OnCollide(uid, component, args.Target);
    }

    private void OnCollide(EntityUid uid, ProjectileDespawnComponent component, EntityUid target)
    {
        if (EntityManager.TryGetComponent<TimeDespawnDamageComponent>(target, out var timeDespawn))
        {
            timeDespawn.Count++;
        }
    }
}
