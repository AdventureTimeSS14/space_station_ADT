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
    [Dependency] private readonly HumanoidProfileSystem _humanoid = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
    [Dependency] private readonly GrammarSystem _grammar = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;

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

        if (TryComp<VisualBodyComponent>(entity, out var _))
        {
            _visualBody.ApplyProfile(entity, new OrganProfileData { Sex = newSex });
        }
    }
}