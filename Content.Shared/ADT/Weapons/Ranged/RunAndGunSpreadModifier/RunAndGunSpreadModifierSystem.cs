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
        SubscribeLocalEvent<RunAndGunSpreadModifierComponent, GunShotEvent>(OnModifyAngle);
        SubscribeLocalEvent<RunAndGunSpreadModifierComponent, AttemptShootEvent>(OnBlocker);
    }
    private void OnModifyAngle(Entity<RunAndGunSpreadModifierComponent> ent, ref GunShotEvent args)
    {
        if (!TryComp<PhysicsComponent>(args.User, out var physics) || physics.LinearVelocity.LengthSquared() < 1)
            return;
        var offset = new Vector2(_random.NextFloat(-physics.LinearVelocity.X, physics.LinearVelocity.X), _random.NextFloat(-physics.LinearVelocity.Y, physics.LinearVelocity.Y));
        var toCoordinates = args.ToCoordinates.Offset(offset);

        args.ToCoordinates = toCoordinates;
    }
    private void OnBlocker(Entity<RunAndGunSpreadModifierComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled)
            return;
        if (!TryComp<PhysicsComponent>(args.User, out var physics) || physics.LinearVelocity.LengthSquared() > 0)
        {
            args.Cancelled = true;
        }
    }
}
