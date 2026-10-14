using System.Linq;
using System.Text.RegularExpressions;
using Content.Server.ADT.Salvage.Systems;
using Content.Server.Chat;
using Content.Shared.ADT.Deafness;
using Content.Shared.ADT.Language;
using Content.Shared.ADT.VoiceOfGod;
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Roles.Jobs;
using Content.Shared.Speech.Muting;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using NewStatusEffectsSystem = Content.Shared.StatusEffectNew.StatusEffectsSystem;

namespace Content.Server.ADT.VoiceOfGod;

public sealed class ADTVoiceOfGodSystem : SharedADTVoiceOfGodSystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedEntityConditionsSystem _conditions = default!;
    [Dependency] private SharedEntityEffectsSystem _effects = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MegafaunaSystem _megafauna = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private NewStatusEffectsSystem _statusEffects = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly List<(ADTVoiceOfGodCommandPrototype Command, Regex Words)> _commands = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTVoiceOfGodComponent, ADTVoiceOfGodActionEvent>(OnAction);
        SubscribeLocalEvent<ADTVoiceOfGodSpeakerComponent, AlternativeSpeechEvent>(OnSpeech);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        LoadCommands();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<ADTVoiceOfGodCommandPrototype>())
            LoadCommands();
    }

    private void LoadCommands()
    {
        _commands.Clear();

        foreach (var command in _proto.EnumeratePrototypes<ADTVoiceOfGodCommandPrototype>().OrderByDescending(command => command.Priority))
        {
            _commands.Add((command, new Regex($@"\b(?:{string.Join("|", command.Words)})\b", RegexOptions.Compiled)));
        }
    }

    private void OnAction(Entity<ADTVoiceOfGodComponent> ent, ref ADTVoiceOfGodActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ent.Comp.Active = !ent.Comp.Active;
        _actions.SetToggled(ent.Comp.ActionUid, ent.Comp.Active);

        var message = ent.Comp.Active ? "adt-voice-of-god-enabled" : "adt-voice-of-god-disabled";
        _popup.PopupEntity(Loc.GetString(message), args.Performer, args.Performer);
    }

    private void OnSpeech(Entity<ADTVoiceOfGodSpeakerComponent> ent, ref AlternativeSpeechEvent args)
    {
        if (args.Cancelled || args.Radio || args.Type != InGameICChatType.Speak)
            return;

        if (!TryComp<ADTVoiceOfGodComponent>(ent.Comp.Organ, out var voice) || !voice.Active)
            return;

        var now = _timing.CurTime;

        if (now < voice.NextCommand)
        {
            var seconds = (int) Math.Ceiling((voice.NextCommand - now).TotalSeconds);
            _popup.PopupEntity(Loc.GetString("adt-voice-of-god-cooldown", ("seconds", seconds)), ent, ent);
            return;
        }

        if (!CanSpeak(ent))
            return;

        var command = args.Message.Trim();

        if (command.Length == 0)
            return;

        args.Cancel();
        Speak((ent.Comp.Organ, voice), ent, command);
    }

    private bool CanSpeak(EntityUid user)
    {
        if (_mobState.IsIncapacitated(user))
            return false;

        return !_statusEffects.HasEffectComp<MutedStatusEffectComponent>(user);
    }

    private void Speak(Entity<ADTVoiceOfGodComponent> ent, EntityUid user, string command)
    {
        _megafauna.SpeakLine(user, command.ToUpperInvariant(), "Default", 16, ent.Comp.SpeechColor, true);
        _audio.PlayPvs(ent.Comp.Sound, user);

        var message = command.ToLowerInvariant();
        var listeners = GetListeners(ent, user);

        if (listeners.Count == 0)
        {
            SetCooldown(ent, ADTVoiceOfGodCooldown.None);
            return;
        }

        var power = ApplyModifiers(ent.Comp.BaseMultiplier, ent.Comp.PowerModifiers, user);
        message = FocusListeners(listeners, message, ref power);

        var cooldown = Execute(user, listeners, message, power);
        SetCooldown(ent, cooldown);

        var names = string.Join(", ", listeners.Select(listener => ToPrettyString(listener).ToString()));
        _adminLog.Add(
            LogType.Action,
            LogImpact.High,
            $"{ToPrettyString(user):user} said '{command.ToUpperInvariant()}' with a Voice of God, affecting {names}, with a power multiplier of {power}");
    }

    private List<EntityUid> GetListeners(Entity<ADTVoiceOfGodComponent> ent, EntityUid user)
    {
        var listeners = new List<EntityUid>();

        foreach (var mob in _lookup.GetEntitiesInRange<MobStateComponent>(Transform(user).Coordinates, ent.Comp.Range))
        {
            if (mob.Owner == user || _mobState.IsDead(mob.Owner, mob.Comp))
                continue;

            if (HasComp<ADTDeafenedComponent>(mob) || HasComp<DeafTraitComponent>(mob))
                continue;

            if (!_examine.InRangeUnOccluded(user, mob.Owner, ent.Comp.Range))
                continue;

            listeners.Add(mob.Owner);
        }

        return listeners;
    }

    private float ApplyModifiers(float power, List<ADTVoiceOfGodPowerModifier> modifiers, EntityUid user)
    {
        foreach (var modifier in modifiers)
        {
            if (_conditions.TryAnyCondition(user, modifier.AnyOf))
                power *= modifier.Multiplier;
        }

        return power;
    }

    private string FocusListeners(List<EntityUid> listeners, string message, ref float power)
    {
        var specific = new List<EntityUid>();
        string? found = null;

        foreach (var listener in listeners)
        {
            var name = Name(listener).ToLowerInvariant();

            if (name.Length > 0 && message.StartsWith(name))
            {
                specific.Add(listener);
                found = name;
                continue;
            }

            if (_mind.TryGetMind(listener, out var mindId, out _)
                && _jobs.MindTryGetJob(mindId, out var job)
                && job.LocalizedName.ToLowerInvariant() is { Length: > 0 } jobName
                && message.StartsWith(jobName))
            {
                specific.Add(listener);
                found = jobName;
            }
        }

        if (specific.Count == 0 || found == null)
            return message;

        listeners.Clear();
        listeners.AddRange(specific);
        power *= 1f + 1f / specific.Count;

        return message.Substring(found.Length);
    }

    private ADTVoiceOfGodCooldown Execute(EntityUid user, List<EntityUid> listeners, string message, float power)
    {
        foreach (var (command, words) in _commands)
        {
            if (!words.IsMatch(message))
                continue;

            if (command.Sound != null)
                _audio.PlayPvs(command.Sound, user);

            if (!_conditions.TryConditions(user, command.UserConditions))
                return command.FailCooldown;

            var commandPower = ApplyModifiers(power, command.PowerModifiers, user);

            foreach (var listener in listeners)
            {
                _effects.ApplyEffects(listener, command.Effects, commandPower, user);
            }

            return command.Cooldown;
        }

        return ADTVoiceOfGodCooldown.None;
    }

    private void SetCooldown(Entity<ADTVoiceOfGodComponent> ent, ADTVoiceOfGodCooldown cooldown)
    {
        var delay = cooldown switch
        {
            ADTVoiceOfGodCooldown.Stun => ent.Comp.CooldownStun,
            ADTVoiceOfGodCooldown.Damage => ent.Comp.CooldownDamage,
            ADTVoiceOfGodCooldown.Meme => ent.Comp.CooldownMeme,
            _ => ent.Comp.CooldownNone,
        };

        ent.Comp.NextCommand = _timing.CurTime + delay;
    }
}
