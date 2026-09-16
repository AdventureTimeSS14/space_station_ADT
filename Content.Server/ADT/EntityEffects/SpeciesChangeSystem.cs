using Content.Shared.ADT.EntityEffects;
using Content.Server.Polymorph.Systems;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Polymorph;
using Content.Server.Polymorph.Components;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class SpeciesChangeSystem : EntityEffectSystem<HumanoidProfileComponent, SpeciesChange>
{
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly HumanoidProfileSystem _humanoid = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
    [Dependency] private readonly GrammarSystem _grammar = default!;

    protected override void Effect(Entity<HumanoidProfileComponent> entity, ref EntityEffectEvent<SpeciesChange> args)
    {
        var uid = entity.Owner;
        var effect = args.Effect;

        if (!_prototype.TryIndex(effect.NewSpecies, out var species))
            return;

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

        var result = _polymorph.PolymorphEntity(uid, config);
        if (result.HasValue)
        {
            // Удаляем компонент, используя правильный тип
            if (TryComp<PolymorphedEntityComponent>(result.Value, out _))
                RemCompDeferred<PolymorphedEntityComponent>(result.Value);

            PreserveSexGender(result.Value, entity.Comp);
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