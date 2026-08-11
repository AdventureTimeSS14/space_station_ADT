using Content.Shared.Projectiles;
using Robust.Shared.Physics.Events;

namespace Content.Shared.ADT.Hierophant.Effects;

public sealed class HierophantWallSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HierophantWallComponent, PreventCollideEvent>(OnPreventCollide);
    }

    private void OnPreventCollide(Entity<HierophantWallComponent> ent, ref PreventCollideEvent args)
    {
        if (ent.Comp.Caster == null)
            return;

        var other = args.OtherEntity;

        if (other == ent.Comp.Caster.Value)
        {
            args.Cancelled = true;
            return;
        }

        if (TryComp<ProjectileComponent>(other, out var projectile) && projectile.Shooter == ent.Comp.Caster)
            args.Cancelled = true;
    }
}
