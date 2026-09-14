using Content.Shared.Verbs;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Xenobiology.Potions;

/// <summary>
/// A potion that changes the gender of a humanoid. The desired gender is picked via verbs.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlimeGenderChangePotionComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public Gender? Gender;
}

public sealed partial class SlimeGenderChangePotionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeGenderChangePotionComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
    }

    private void OnGetVerbs(Entity<SlimeGenderChangePotionComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var category = new VerbCategory(Loc.GetString("xeno-potion-gender-category"), null, false);

        AddGenderVerb(args, ent, category, Gender.Neuter, "xeno-potion-gender-neuter");
        AddGenderVerb(args, ent, category, Gender.Epicene, "xeno-potion-gender-epicene");
        AddGenderVerb(args, ent, category, Gender.Female, "xeno-potion-gender-female");
        AddGenderVerb(args, ent, category, Gender.Male, "xeno-potion-gender-male");
    }

    private void AddGenderVerb(GetVerbsEvent<InteractionVerb> args, Entity<SlimeGenderChangePotionComponent> ent,
        VerbCategory category, Gender gender, string locKey)
    {
        var verb = new InteractionVerb
        {
            Text = Loc.GetString(locKey),
            Act = () => ent.Comp.Gender = gender,
            Disabled = ent.Comp.Gender == gender,
            Category = category,
        };

        args.Verbs.Add(verb);
    }
}
