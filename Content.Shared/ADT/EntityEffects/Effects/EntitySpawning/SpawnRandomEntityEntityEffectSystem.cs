using Content.Shared.EntityEffects;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.EntityEffects.Effects.EntitySpawning;

/// <summary>
/// Spawns a random entity from a list of prototypes at the coordinates of this entity.
/// Amount is modified by scale.
/// </summary>
public sealed partial class SpawnRandomEntityEntityEffectSystem : EntityEffectSystem<TransformComponent, SpawnRandomEntity>
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<SpawnRandomEntity> args)
    {
        var quantity = args.Effect.Number * (int)Math.Floor(args.Scale);

        if (!args.Effect.Predicted && !_net.IsServer)
            return;

        if (args.Effect.Prototypes == null || args.Effect.Prototypes.Count == 0)
            return;

        for (var i = 0; i < quantity; i++)
        {
            var protoId = _random.Pick(args.Effect.Prototypes);
            if (args.Effect.Predicted)
            {
                PredictedSpawnNextToOrDrop(protoId, entity, entity.Comp);
            }
            else
            {
                SpawnNextToOrDrop(protoId, entity, entity.Comp);
            }
        }
    }
}

/// <summary>
/// Effect that spawns a random entity from a list of prototypes.
/// </summary>
[DataDefinition]
public sealed partial class SpawnRandomEntity : EntityEffectBase<SpawnRandomEntity>
{
    /// <summary>
    /// Amount of entities we're spawning
    /// </summary>
    [DataField]
    public int Number = 1;

    /// <summary>
    /// List of prototypes to randomly choose from when spawning.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Prototypes = new();

    /// <summary>
    /// Whether this spawning is predicted. Set false to not predict the spawn.
    /// Entities with animations or that have random elements when spawned should set this to false.
    /// </summary>
    [DataField]
    public bool Predicted = true;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("entity-effect-guidebook-spawn-entity",
            ("chance", Probability),
            ("entname", Loc.GetString("entity-effect-random-item")),
            ("amount", Number));
}
