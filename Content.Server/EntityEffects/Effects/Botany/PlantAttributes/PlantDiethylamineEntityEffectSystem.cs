using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Botany.PlantAttributes;
using Robust.Shared.Random;

namespace Content.Server.EntityEffects.Effects.Botany.PlantAttributes;

public sealed partial class PlantDiethylamineEntityEffectSystem : EntityEffectSystem<PlantHolderComponent, PlantDiethylamine>
{
    [Dependency] private readonly IRobustRandom _random = default!;
<<<<<<< HEAD
    [Dependency] private readonly PlantHolderSystem _plantHolder = default!;
=======
>>>>>>> upstreamwiz/master

    protected override void Effect(Entity<PlantHolderComponent> entity, ref EntityEffectEvent<PlantDiethylamine> args)
    {
        if (entity.Comp.Seed == null || entity.Comp.Dead || entity.Comp.Seed.Immutable)
            return;

        if (_random.Prob(0.1f))
        {
<<<<<<< HEAD
            _plantHolder.EnsureUniqueSeed(entity, entity);
=======
>>>>>>> upstreamwiz/master
            entity.Comp.Seed!.Lifespan++;
        }

        if (_random.Prob(0.1f))
        {
<<<<<<< HEAD
            _plantHolder.EnsureUniqueSeed(entity, entity);
=======
>>>>>>> upstreamwiz/master
            entity.Comp.Seed!.Endurance++;
        }
    }
}
