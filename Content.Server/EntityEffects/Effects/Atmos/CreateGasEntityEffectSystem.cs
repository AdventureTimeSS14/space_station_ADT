using Content.Server.ADT.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Atmos;

namespace Content.Server.EntityEffects.Effects.Atmos;

/// <summary>
/// This effect adjusts a gas at the tile this entity is currently on.
/// The amount changed is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class CreateGasEntityEffectSystem : EntityEffectSystem<TransformComponent, CreateGas>
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private ADTReactionGasReleaseSystem _gasRelease = default!; // ADT-Tweak

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<CreateGas> args)
    {
        var tileMix = _gasRelease.GetReleaseMixture(entity); // ADT-Tweak

        tileMix?.AdjustMoles(args.Effect.Gas, args.Scale * args.Effect.Moles);
    }
}
