using Content.Shared.ADT.Crawling.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared.ADT.Damage;

public sealed class ADTProjectilePassDeadSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTProjectilePassDeadComponent, PreventCollideEvent>(OnPreventCollide);
    }

    private void OnPreventCollide(Entity<ADTProjectilePassDeadComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled || !_mobState.IsDead(ent.Owner))
            return;

        var other = args.OtherEntity;

        if (!HasComp<ProjectileComponent>(other))
            return;

        if (HasComp<ProjectileIgnoreCrawlingComponent>(other))
            return;

        if (ent.Comp.AllowTargeted &&
            TryComp<TargetedProjectileComponent>(other, out var targeted) &&
            targeted.Target == ent.Owner)
        {
            return;
        }

        args.Cancelled = true;
    }
}
