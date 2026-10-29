using Content.Server.Explosion.EntitySystems;
using Content.Shared.ADT.Lavaland;
using Content.Shared.ADT.Lavaland.Components;
using Content.Shared.Explosion.Components;

namespace Content.Server.ADT.Lavaland;

public sealed class ADTGoliathPlatingSystem : SharedADTGoliathPlatingSystem
{
    [Dependency] private ExplosionSystem _explosion = default!;

    protected override void OnPlated(Entity<ADTGoliathPlatableComponent> ent)
    {
        if (ent.Comp.ExplosionStep <= 0f || !TryComp<ExplosionResistanceComponent>(ent, out var resistance))
            return;

        var coefficient = MathF.Max(resistance.DamageCoefficient - ent.Comp.ExplosionStep, ent.Comp.MinExplosionCoefficient);
        _explosion.SetExplosionResistance(ent, coefficient, resistance);
    }
}
