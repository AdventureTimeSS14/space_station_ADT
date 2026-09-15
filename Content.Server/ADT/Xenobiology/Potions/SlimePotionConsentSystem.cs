using Content.Server.EUI;
using Content.Shared;
using Content.Shared.ADT.Language;
using Content.Shared.ADT.Xenobiology.Potions;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Popups;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Server.ADT.Xenobiology.Potions;

public sealed partial class SlimePotionConsentSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly HumanoidProfileSystem _humanoid = default!;
    [Dependency] private readonly SharedLanguageSystem _language = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimeMindTransferencePotionComponent, AfterInteractEvent>(OnMindTransferenceUse);
        SubscribeLocalEvent<SlimeNameChangePotionComponent, AfterInteractEvent>(OnNameChangeUse);
        SubscribeLocalEvent<SlimeGenderChangePotionComponent, AfterInteractEvent>(OnGenderChangeUse);
    }

    private void OnMindTransferenceUse(Entity<SlimeMindTransferencePotionComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target || !args.CanReach)
            return;

        if (!TryComp<MindContainerComponent>(args.User, out var userMindContainer))
            return;

        if (!TryComp<MindContainerComponent>(target, out var targetMindContainer))
            return;

        if (userMindContainer.Mind is not { } mind)
            return;

        args.Handled = true;

        if (targetMindContainer.HasMind)
        {
            if (!TryGetSession(target, out var session))
                return;

            _eui.OpenEui(new AcceptMindTransferenceEui(args.User, target, targetMindContainer.Mind!.Value, args.Used, Name(args.User), this), session);
            return;
        }

        _mind.TransferTo(mind, target);
        QueueDel(args.Used);
    }

    private void OnNameChangeUse(Entity<SlimeNameChangePotionComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target || !args.CanReach)
            return;

        if (!TryComp<MindContainerComponent>(target, out var targetMindContainer))
            return;

        if (string.IsNullOrWhiteSpace(ent.Comp.AssignedName))
        {
            _popup.PopupEntity(Loc.GetString("xeno-potion-name-not-set"), args.User, args.User);
            return;
        }

        args.Handled = true;

        if (targetMindContainer.HasMind)
        {
            if (!TryGetSession(target, out var session))
                return;

            _eui.OpenEui(new AcceptNameChangeEui(target, targetMindContainer.Mind!.Value, args.Used, ent.Comp.AssignedName, Name(args.User), this), session);
            return;
        }

        var oldName = Name(target);
        _metaData.SetEntityName(target, ent.Comp.AssignedName);
        _popup.PopupEntity(Loc.GetString("xeno-potion-name-renamed", ("old", oldName), ("new", ent.Comp.AssignedName)), args.User, args.User);
        QueueDel(args.Used);
    }

    private void OnGenderChangeUse(Entity<SlimeGenderChangePotionComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target || !args.CanReach)
            return;

        if (!TryComp<HumanoidProfileComponent>(target, out var profile))
            return;

        args.Handled = true;

        if (ent.Comp.Gender is not { } gender)
        {
            _popup.PopupEntity(Loc.GetString("xeno-potion-gender-not-selected"), args.User, args.User);
            return;
        }

        var genderText = Loc.GetString($"xeno-potion-gender-{gender.ToString().ToLowerInvariant()}");

        if (profile.Gender == gender)
        {
            _popup.PopupEntity(Loc.GetString("xeno-potion-gender-already", ("gender", genderText)), args.User, args.User);
            return;
        }

        if (TryComp<MindContainerComponent>(target, out var targetMind) && targetMind.HasMind)
        {
            if (!TryGetSession(target, out var session))
                return;

            _eui.OpenEui(new AcceptGenderChangeEui(target, targetMind.Mind!.Value, args.Used, gender, Name(args.User), this), session);
            return;
        }

        _humanoid.SetGender((target, profile), gender);
        _popup.PopupEntity(Loc.GetString("xeno-potion-gender-applied", ("gender", genderText)), args.User, args.User);
        QueueDel(args.Used);
    }

    private bool TryGetSession(EntityUid target, out ICommonSession session)
    {
        session = null!;
        if (!_mind.TryGetMind(target, out _, out var mind) || mind.UserId is not { } userId)
            return false;

        if (!_player.TryGetSessionById(userId, out var found))
            return false;

        session = found;
        return true;
    }

    public bool ValidateTargetMind(EntityUid target, EntityUid expectedMind)
    {
        return !Deleted(target) && _mind.TryGetMind(target, out var currentMind, out _) && currentMind == expectedMind;
    }

    public void DoMindSwap(EntityUid user, EntityUid target, EntityUid potion)
    {
        if (Deleted(potion) || !_mind.TryGetMind(user, out var userMindId, out _) ||
            !_mind.TryGetMind(target, out var targetMindId, out _))
            return;

        _mind.TransferTo(targetMindId, user, ghostCheckOverride: true);
        _mind.TransferTo(userMindId, target, ghostCheckOverride: true);
        SwapLanguages(user, target);

        _popup.PopupEntity(Loc.GetString("xeno-potion-mind-swap-target", ("user", Name(user))), user, user);
        _popup.PopupEntity(Loc.GetString("xeno-potion-mind-swap-user", ("target", Name(target))), target, target);
        QueueDel(potion);
    }

    private void SwapLanguages(EntityUid a, EntityUid b)
    {
        var compA = EnsureComp<LanguageSpeakerComponent>(a);
        var compB = EnsureComp<LanguageSpeakerComponent>(b);

        (compA.CurrentLanguage, compB.CurrentLanguage) = (compB.CurrentLanguage, compA.CurrentLanguage);
        (compA.Languages, compB.Languages) = (compB.Languages, compA.Languages);

        Dirty(a, compA);
        Dirty(b, compB);
        _language.UpdateUi(a);
        _language.UpdateUi(b);
    }

    public void DoRename(EntityUid target, string newName, EntityUid potion)
    {
        if (Deleted(potion) || !Exists(target))
            return;

        _metaData.SetEntityName(target, newName);
        _popup.PopupEntity(Loc.GetString("xeno-potion-name-you-are", ("name", newName)), target, target);
        QueueDel(potion);
    }

    public void DoGenderChange(EntityUid target, Gender gender, EntityUid potion)
    {
        if (Deleted(potion) || !TryComp<HumanoidProfileComponent>(target, out var profile))
            return;

        _humanoid.SetGender((target, profile), gender);
        _popup.PopupEntity(Loc.GetString("xeno-potion-gender-applied",
            ("gender", Loc.GetString($"xeno-potion-gender-{gender.ToString().ToLowerInvariant()}"))), target, target);
        QueueDel(potion);
    }
}