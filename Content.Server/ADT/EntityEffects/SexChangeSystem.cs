using System.Linq;
using Content.Server.Body;
using Content.Shared.ADT.EntityEffects;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;

namespace Content.Server.ADT.EntityEffects;

public sealed partial class SexChangeSystem : EntityEffectSystem<HumanoidProfileComponent, SexChange>
{
    [Dependency] private HumanoidProfileSystem _humanoid = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;
    [Dependency] private GrammarSystem _grammar = default!;
    [Dependency] private IdentitySystem _identity = default!;

    protected override void Effect(Entity<HumanoidProfileComponent> entity, ref EntityEffectEvent<SexChange> args)
    {
        Sex newSex;

        if (args.Effect.NewSex.HasValue)
        {
            newSex = args.Effect.NewSex.Value;
        }
        else
        {
            newSex = entity.Comp.Sex == Sex.Male ? Sex.Female : Sex.Male;
        }

        _humanoid.SetSex((entity, entity.Comp), newSex);

        var newGender = newSex switch
        {
            Sex.Male => Gender.Male,
            Sex.Female => Gender.Female,
            Sex.Unsexed => Gender.Epicene,
            _ => entity.Comp.Gender
        };

        if (newGender != entity.Comp.Gender)
        {
            _humanoid.SetGender((entity, entity.Comp), newGender);
        }

        if (TryComp<GrammarComponent>(entity, out var grammar))
        {
            _grammar.SetGender((entity, grammar), entity.Comp.Gender);
        }

        _identity.QueueIdentityUpdate(entity);

        if (_visualBody.TryGatherMarkingsData(entity.Owner, null, out var profiles, out _, out _))
        {
            _visualBody.ApplyProfiles(entity, profiles.ToDictionary(pair => pair.Key, pair => pair.Value with { Sex = newSex }));
        }
    }
}