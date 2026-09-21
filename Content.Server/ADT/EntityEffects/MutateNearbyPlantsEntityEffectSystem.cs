using Content.Shared.ADT.EntityEffects;
using Content.Shared.EntityEffects;
using Robust.Shared.Map;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class MutateNearbyPlantsEntityEffectSystem : EntityEffectSystem<TransformComponent, MutateNearbyPlantsEntityEffect>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<MutateNearbyPlantsEntityEffect> args)
    {
        // TODO: Реализовать мутацию растений
        // Пока мне лень
    }
}