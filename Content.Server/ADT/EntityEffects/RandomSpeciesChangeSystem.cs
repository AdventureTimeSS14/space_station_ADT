using Content.Shared.ADT.EntityEffects;
using Content.Server.Polymorph.Systems;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Polymorph;
using Content.Server.Polymorph.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class RandomSpeciesChangeSystem : EntityEffectSystem<HumanoidProfileComponent, RandomSpeciesChange>
{
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<HumanoidProfileComponent> entity, ref EntityEffectEvent<RandomSpeciesChange> args)
    {
        var uid = entity.Owner;
        var effect = args.Effect;

        var species = _prototype.EnumeratePrototypes<SpeciesPrototype>().ToList();

        if (effect.SpeciesWhitelist != null && effect.SpeciesWhitelist.Count > 0)
            species = species.Where(q => effect.SpeciesWhitelist.Any(w => q.ID == w)).ToList();

        if (effect.SpeciesBlacklist != null && effect.SpeciesBlacklist.Count > 0)
            species = species.Where(q => !effect.SpeciesBlacklist.Any(w => q.ID == w)).ToList();

        if (species.Count == 0)
            return;

        var selectedSpecies = _random.Pick(species);

        var config = new PolymorphConfiguration
        {
            Entity = selectedSpecies.Prototype,
            TransferDamage = true,
            Forced = true,
            Inventory = PolymorphInventoryChange.Transfer,
            RevertOnCrit = false,
            RevertOnDeath = false,
            TransferName = true,
        };

        var result = _polymorph.PolymorphEntity(uid, config);
        if (result.HasValue)
        {
            if (TryComp<PolymorphedEntityComponent>(result.Value, out _))
                RemCompDeferred<PolymorphedEntityComponent>(result.Value);
        }
    }
}