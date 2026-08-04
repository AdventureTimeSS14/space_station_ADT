using Content.Shared.ADT.EntityEffects;
using Content.Server.Polymorph.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Polymorph;
using Content.Server.Polymorph.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class ScrambleNearbyEffectSystem : EntityEffectSystem<TransformComponent, ScrambleNearbyEffect>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<ScrambleNearbyEffect> args)
    {
        var uid = entity.Owner;
        var effect = args.Effect;

        var allSpecies = new List<string>();
        foreach (var species in _prototype.EnumeratePrototypes<SpeciesPrototype>())
        {
            if (species.ID == "Skeleton" || species.ID == "IPC" || species.ID == "Cyborg")
                continue;
            allSpecies.Add(species.ID);
        }

        if (allSpecies.Count == 0)
            return;

        foreach (var target in _lookup.GetEntitiesInRange(uid, effect.Radius))
        {
            if (!TryComp<HumanoidProfileComponent>(target, out _))
                continue;

            var randomSpecies = _random.Pick(allSpecies);
            if (!_prototype.TryIndex<SpeciesPrototype>(randomSpecies, out var species))
                continue;

            var config = new PolymorphConfiguration
            {
                Entity = species.Prototype,
                TransferDamage = true,
                Forced = true,
                Inventory = PolymorphInventoryChange.Transfer,
                RevertOnCrit = false,
                RevertOnDeath = false,
                TransferName = true,
            };

            var result = _polymorph.PolymorphEntity(target, config);
            if (result.HasValue)
            {
                if (TryComp<PolymorphedEntityComponent>(result.Value, out _))
                    RemCompDeferred<PolymorphedEntityComponent>(result.Value);
            }
        }
    }
}