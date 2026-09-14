using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Popups;
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
    [Dependency] private readonly HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeGenderChangePotionComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SlimeGenderChangePotionComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
    }

    private void OnAfterInteract(Entity<SlimeGenderChangePotionComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target || !args.CanReach)
            return;

        if (!TryComp<HumanoidProfileComponent>(target, out var profile))
            return;

        args.Handled = true;

        if (!ent.Comp.Gender.HasValue)
        {
            _popup.PopupPredicted(Loc.GetString("xeno-potion-gender-not-selected"), args.User, args.User);
            return;
        }

        var genderLocKey = $"xeno-potion-gender-{ent.Comp.Gender.Value.ToString().ToLowerInvariant()}";
        var genderText = Loc.GetString(genderLocKey);

        if (ent.Comp.Gender.Value == profile.Gender)
        {
            _popup.PopupPredicted(Loc.GetString("xeno-potion-gender-already", ("gender", genderText)), args.User, args.User);
            return;
        }

        _humanoidProfile.SetGender((target, profile), ent.Comp.Gender.Value);
        _popup.PopupPredicted(Loc.GetString("xeno-potion-gender-applied", ("gender", genderText)), args.User, args.User);
        PredictedQueueDel(args.Used);
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
