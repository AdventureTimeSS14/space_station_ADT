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
        if (!TryComp<PhysicsComponent>(args.User, out var physics) || 
            physics.LinearVelocity.LengthSquared() < 1f)
            return;

        var dir = args.ToCoordinates.Position - args.FromCoordinates.Position;
        var lenSq = dir.LengthSquared();
        
        if (lenSq < 0.0001f)
            return;

        var invLen = 1f / MathF.Sqrt(lenSq);
        dir *= invLen;

        var spread = MathF.Abs(physics.LinearVelocity.X + physics.LinearVelocity.Y) * ent.Comp.Modifyer;
        
        args.ToCoordinates = args.ToCoordinates.Offset(new Vector2(
            dir.X * _random.NextFloat(0f, spread) - dir.Y * _random.NextFloat(-spread, spread),
            dir.Y * _random.NextFloat(0f, spread) + dir.X * _random.NextFloat(-spread, spread)
        ));
    }

    private void OnBlocker(Entity<RunAndGunBlockerComponent> ent, ref AttemptShootEvent args)
    {
        if (!args.Cancelled && 
            TryComp<PhysicsComponent>(args.User, out var physics) && 
            physics.LinearVelocity.LengthSquared() > 0f)
            args.Cancelled = true;
    }
}