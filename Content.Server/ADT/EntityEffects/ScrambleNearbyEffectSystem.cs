using System.Linq;
using Content.Shared.ADT.EntityEffects;
using Content.Server.Polymorph.Systems;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Polymorph;
using Content.Server.Polymorph.Components;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class ScrambleNearbyEffectSystem : EntityEffectSystem<TransformComponent, ScrambleNearbyEffect>
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly HumanoidProfileSystem _humanoid = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
    [Dependency] private readonly GrammarSystem _grammar = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<ScrambleNearbyEffect> args)
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

        foreach (var target in _lookup.GetEntitiesInRange(uid, effect.Radius))
        {
            if (!TryComp<HumanoidProfileComponent>(target, out var profile))
                continue;

            if (HasComp<GhostComponent>(target))
                continue;

            if (effect.SpeciesBlacklist != null && effect.SpeciesBlacklist.Contains(profile.Species))
                continue;

            var randomSpecies = _random.Pick(species);

            var config = new PolymorphConfiguration
            {
                Entity = randomSpecies.Prototype,
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

                PreserveSexGender(result.Value, profile);
            }
        }
    }

    private void PreserveSexGender(EntityUid newEntity, HumanoidProfileComponent profile)
    {
        _humanoid.SetSex((newEntity, null), profile.Sex);
        _humanoid.SetGender((newEntity, null), profile.Gender);

        if (_visualBody.TryGatherMarkingsData(newEntity, null, out var organProfiles, out _, out _))
        {
            foreach (var category in organProfiles.Keys)
                organProfiles[category] = organProfiles[category] with { Sex = profile.Sex };

            _visualBody.ApplyProfiles(newEntity, organProfiles);
        }

        if (TryComp<GrammarComponent>(newEntity, out var grammar))
            _grammar.SetGender((newEntity, grammar), profile.Gender);
    }
}
