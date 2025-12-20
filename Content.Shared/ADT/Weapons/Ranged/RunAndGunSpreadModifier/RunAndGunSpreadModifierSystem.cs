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
            physics.LinearVelocity.Length() < ent.Comp.MinVelocity)
            return;
        // Log.Warning(physics.LinearVelocity.LengthSquared().ToString()); //раскомментируйте, если хотите заменять скорость для нового оружия

        var dir = args.ToCoordinates.Position - args.FromCoordinates.Position;

        var spread = physics.LinearVelocity.Length() * ent.Comp.Modifier / 4;

        args.ToCoordinates = args.ToCoordinates.Offset(new Vector2(
            dir.X * _random.NextFloat(0f, spread) - dir.Y * _random.NextFloat(-spread, spread),
            dir.Y * _random.NextFloat(0f, spread) + dir.X * _random.NextFloat(-spread, spread)
        ));
    }

    private void OnBlocker(Entity<RunAndGunBlockerComponent> ent, ref AttemptShootEvent args)
    {
        if (!args.Cancelled &&
            TryComp<PhysicsComponent>(args.User, out var physics) &&
            physics.LinearVelocity.Length() > ent.Comp.MaxVelocity)
            args.Cancelled = true;
    }
}