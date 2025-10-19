using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Physics.Components;
using System.Numerics;
using Robust.Shared.Random;

namespace Content.Shared.ADT.Weapons.Ranged.RunAndGunSpreadModifier;

public sealed class RunAndGunSpreadModifierSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RunAndGunSpreadModifierComponent, GunShotEvent>(OnModifyAngle);
        SubscribeLocalEvent<RunAndGunBlockerComponent, AttemptShootEvent>(OnBlocker);
    }
    private void OnModifyAngle(Entity<RunAndGunSpreadModifierComponent> ent, ref GunShotEvent args)
    {
        if (!TryComp<PhysicsComponent>(args.User, out var physics))
            return;

        var vel = physics.LinearVelocity;
        if (vel.LengthSquared() < 1f)
            return;

        var mod = ent.Comp.Modifyer;
        args.ToCoordinates = args.ToCoordinates.Offset(new Vector2(
            _random.NextFloat(-vel.X * mod, vel.X * mod),
            _random.NextFloat(-vel.Y * mod, vel.Y * mod)
        ));
    }

    private void OnBlocker(Entity<RunAndGunBlockerComponent> ent, ref AttemptShootEvent args)
    {
        if (!args.Cancelled &&
            TryComp<PhysicsComponent>(args.User, out var physics) &&
            physics.LinearVelocity.LengthSquared() > 0f)
        {
            args.Cancelled = true;
        }
    }
}