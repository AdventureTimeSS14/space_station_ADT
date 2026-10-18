using System.Linq;
using System.Text;
using Content.Server.ADT.Dissociative.Components;
using Content.Server.ADT.MidroundCustomization;
using Content.Shared.ADT.Dissociative;
using Content.Shared.ADT.MidroundCustomization;
using Content.Shared.ADT.TTS;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Player;

namespace Content.Server.ADT.Dissociative;

public sealed partial class DissociativeIdentitySystem
{
    [Dependency] private GrammarSystem _grammar = default!;

    private const int MaxAlterNameLength = 32;

    private void InitializeSetup()
    {
        SubscribeLocalEvent<DissociativeAlterComponent, DissociativeSetupActionEvent>(OnSetupAction);

        Subs.BuiEvents<DissociativeAlterComponent>(DissociativeAlterSetupUiKey.Key, subs =>
        {
            subs.Event<DissociativeAlterSetSexMessage>(OnSetSex);
            subs.Event<DissociativeAlterOpenCustomizationMessage>(OnOpenCustomization);
            subs.Event<DissociativeAlterConfirmMessage>(OnConfirm);
        });
    }

    private bool CanSetup(Entity<DissociativeAlterComponent> ent, EntityUid actor)
    {
        return !ent.Comp.SetupDone &&
               actor == ent.Owner &&
               TryGetHost(ent, out var host) &&
               !host.Value.Comp.Fronting;
    }

    private void OnSetupAction(Entity<DissociativeAlterComponent> ent, ref DissociativeSetupActionEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(ent, out var actor))
            return;

        args.Handled = true;
        OpenSetup(ent, actor.PlayerSession);
    }

    private void OpenSetup(Entity<DissociativeAlterComponent> ent, ICommonSession session)
    {
        if (!CanSetup(ent, ent.Owner))
            return;

        _ui.OpenUi(ent.Owner, DissociativeAlterSetupUiKey.Key, session);
        UpdateSetupState(ent);
    }

    private void UpdateSetupState(Entity<DissociativeAlterComponent> ent)
    {
        var sex = TryComp<HumanoidProfileComponent>(ent, out var profile) ? profile.Sex : Sex.Unsexed;
        _ui.SetUiState(ent.Owner, DissociativeAlterSetupUiKey.Key, new DissociativeAlterSetupUiState(Name(ent), sex));
    }

    private void OnSetSex(Entity<DissociativeAlterComponent> ent, ref DissociativeAlterSetSexMessage args)
    {
        if (!CanSetup(ent, args.Actor) ||
            !TryComp<HumanoidProfileComponent>(ent, out var profile) ||
            !_proto.Resolve(profile.Species, out var species) ||
            !species.Sexes.Contains(args.Sex))
            return;

        var gender = args.Sex switch
        {
            Sex.Male => Gender.Male,
            Sex.Female => Gender.Female,
            _ => Gender.Epicene,
        };

        ApplySex((ent, profile), args.Sex, gender);

        EnsureVoiceFits((ent, profile));
        UpdateSetupState(ent);
    }

    private void ApplySex(Entity<HumanoidProfileComponent> ent, Sex sex, Gender gender)
    {
        _humanoidProfile.SetSex(ent.AsNullable(), sex);
        _humanoidProfile.SetGender(ent.AsNullable(), gender);

        if (TryComp<GrammarComponent>(ent, out var grammar))
            _grammar.SetGender((ent, grammar), gender);

        if (!_visualBody.TryGatherMarkingsData(ent.Owner, null, out var profiles, out _, out _))
            return;

        foreach (var organ in profiles.Keys.ToList())
        {
            var data = profiles[organ];
            data.Sex = sex;
            profiles[organ] = data;
        }

        _visualBody.ApplyProfiles(ent, profiles);
    }

    private void EnsureVoiceFits(Entity<HumanoidProfileComponent> ent)
    {
        if (!TryComp<TTSComponent>(ent, out var tts))
            return;

        if (tts.VoicePrototypeId is { } current &&
            _proto.Resolve(current, out var currentProto) &&
            HumanoidCharacterProfile.CanHaveVoice(currentProto, ent.Comp.Sex, ent.Comp.Species))
            return;

        var fallback = _proto.EnumeratePrototypes<TTSVoicePrototype>()
            .FirstOrDefault(v => v.RoundStart && !v.SponsorOnly && HumanoidCharacterProfile.CanHaveVoice(v, ent.Comp.Sex, ent.Comp.Species));

        if (fallback != null)
            tts.VoicePrototypeId = fallback.ID;
    }

    private void OnOpenCustomization(Entity<DissociativeAlterComponent> ent, ref DissociativeAlterOpenCustomizationMessage args)
    {
        if (!CanSetup(ent, args.Actor) || !HasComp<MidroundCustomizationComponent>(ent))
            return;

        _ui.TryOpenUi(ent.Owner, MidroundCustomizationUiKey.Key, args.Actor);
    }

    private void OnConfirm(Entity<DissociativeAlterComponent> ent, ref DissociativeAlterConfirmMessage args)
    {
        if (!CanSetup(ent, args.Actor))
            return;

        var name = SanitizeName(args.Name);
        if (!string.IsNullOrEmpty(name))
        {
            _meta.SetEntityName(ent, name);

            if (_mind.TryGetMind(ent, out _, out var mind))
                mind.CharacterName = name;
        }

        FinishSetup(ent);
        _popup.PopupEntity(Loc.GetString("dissociative-setup-done"), ent, ent);
    }

    private void FinishSetup(Entity<DissociativeAlterComponent> ent)
    {
        ent.Comp.SetupDone = true;

        _ui.CloseUis(ent.Owner);
        RemComp<MidroundCustomizationComponent>(ent);
        _actions.RemoveAction(ent.Owner, ent.Comp.SetupActionEntity);
        ent.Comp.SetupActionEntity = null;
    }

    private static string SanitizeName(string raw)
    {
        var sb = new StringBuilder();
        foreach (var c in (raw ?? string.Empty).Trim())
        {
            if (char.IsLetter(c) || c is ' ' or '-' or '\'')
                sb.Append(c);
        }

        var name = string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return name.Length > MaxAlterNameLength ? name[..MaxAlterNameLength].TrimEnd() : name;
    }
}
