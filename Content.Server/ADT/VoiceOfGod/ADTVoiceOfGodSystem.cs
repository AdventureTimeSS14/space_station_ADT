// ебучий говнокод, требует рефактора, мне лень
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Content.Server.Administration;
using Content.Server.ADT.Hallucinations;
using Content.Server.ADT.Salvage.Systems;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Silicons.Laws;
using Content.Shared.Abilities.Mime;
using Content.Shared.ADT.Deafness;
using Content.Shared.ADT.Language;
using Content.Shared.ADT.VoiceOfGod;
using Content.Shared.Administration.Logs;
using Content.Shared.Atmos.Components;
using Content.Shared.Bible.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Systems;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Medical;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Speech.Muting;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using NewStatusEffectsSystem = Content.Shared.StatusEffectNew.StatusEffectsSystem;

namespace Content.Server.ADT.VoiceOfGod;

public sealed class ADTVoiceOfGodSystem : SharedADTVoiceOfGodSystem
{
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private SharedBuckleSystem _buckle = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private SharedCombatModeSystem _combat = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private HallucinationsSystem _hallucinations = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private MegafaunaSystem _megafauna = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private ISharedPlayerManager _players = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private QuickDialogSystem _quickDialog = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SiliconLawSystem _laws = default!;
    [Dependency] private SleepingSystem _sleeping = default!;
    [Dependency] private NewStatusEffectsSystem _statusEffects = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private VomitSystem _vomit = default!;

    private static readonly Regex StunWords = new("stop|wait|stand still|hold on|halt|стой|стоп|не двигайся|замри", RegexOptions.Compiled);
    private static readonly Regex WeakenWords = new("drop|fall|trip|упади|ослабни|пади", RegexOptions.Compiled);
    private static readonly Regex SleepWords = new("sleep|slumber|засни|усни|спать|спи", RegexOptions.Compiled);
    private static readonly Regex VomitWords = new("vomit|throw up|рви|блюй", RegexOptions.Compiled);
    private static readonly Regex SilenceWords = new("shut up|silence|ssh|quiet|hush|заткнись|молчать|тишина|тихо", RegexOptions.Compiled);
    private static readonly Regex HallucinateWords = new("see the truth|hallucinate|увидь истину|галлюцинируй", RegexOptions.Compiled);
    private static readonly Regex WakeUpWords = new("wake up|awaken|проснись|очнись", RegexOptions.Compiled);
    private static readonly Regex HealWords = new("live|heal|survive|mend|heroes never die|живи|исцелись|выживи|залечись", RegexOptions.Compiled);
    private static readonly Regex HurtWords = new("die|suffer|умри|страдай|познай боль", RegexOptions.Compiled);
    private static readonly Regex BleedWords = new("bleed|кровоточи", RegexOptions.Compiled);
    private static readonly Regex BurnWords = new("burn|ignite|гори|воспламенись|загорись", RegexOptions.Compiled);
    private static readonly Regex RepulseWords = new("shoo|go away|leave me alone|begone|flee|fus ro dah|прочь|уйди|отстань|исчезни|отвали", RegexOptions.Compiled);
    private static readonly Regex WhoAreYouWords = new("who are you|say your name|state your name|identify|кто ты|назовись|представься", RegexOptions.Compiled);
    private static readonly Regex SayMyNameWords = new("say my name|скажи моё имя", RegexOptions.Compiled);
    private static readonly Regex KnockKnockWords = new("knock knock|тук-тук", RegexOptions.Compiled);
    private static readonly Regex StateLawsWords = new("state laws|state your laws|озвучь законы|перечисли законы", RegexOptions.Compiled);
    private static readonly Regex MoveWords = new("move|двигайся|иди|ступай", RegexOptions.Compiled);
    private static readonly Regex WalkWords = new("walk|slow down|ходи|медленнее", RegexOptions.Compiled);
    private static readonly Regex RunWords = new("run|беги", RegexOptions.Compiled);
    private static readonly Regex HelpIntentWords = new("help|помогай|помоги", RegexOptions.Compiled);
    private static readonly Regex HarmIntentWords = new("harm|fight|атакуй|дерись|навреди", RegexOptions.Compiled);
    private static readonly Regex FlipWords = new("flip|rotate|revolve|roll|somersault|крутись|вертись|перевернись|кувыркнись", RegexOptions.Compiled);
    private static readonly Regex RestWords = new("rest|отдыхай", RegexOptions.Compiled);
    private static readonly Regex GetUpWords = new("get up|встань|вставай", RegexOptions.Compiled);
    private static readonly Regex SitWords = new("sit|садись|сядь", RegexOptions.Compiled);
    private static readonly Regex StandWords = new("stand|стой|встань", RegexOptions.Compiled);
    private static readonly Regex JumpWords = new("jump|прыгай", RegexOptions.Compiled);
    private static readonly Regex SaluteWords = new("salute|отдай честь|салютуй", RegexOptions.Compiled);
    private static readonly Regex DeathgaspWords = new("play dead|притворись мёртвым", RegexOptions.Compiled);
    private static readonly Regex ClapWords = new("clap|applaud|хлопай|аплодируй", RegexOptions.Compiled);
    private static readonly Regex HonkWords = new("ho+nk|хонк", RegexOptions.Compiled);
    private static readonly Regex MultispinWords = new("like a record baby|как пластинка, детка", RegexOptions.Compiled);

    private static readonly ProtoId<EmotePrototype> FlipEmote = "Flip";
    private static readonly ProtoId<EmotePrototype> SpinEmote = "Spin";
    private static readonly ProtoId<EmotePrototype> JumpEmote = "Jump";
    private static readonly ProtoId<EmotePrototype> SaluteEmote = "Salute";
    private static readonly ProtoId<EmotePrototype> DeathgaspEmote = "DefaultDeathgasp";
    private static readonly ProtoId<EmotePrototype> ClapEmote = "Clap";

    private static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    private static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    private static readonly ProtoId<DamageTypePrototype> HurtType = "Blunt";

    private static readonly EntProtoId SleepEffect = "StatusEffectForcedSleeping";
    private static readonly EntProtoId MutedEffect = "StatusEffectMuted";
    private static readonly ProtoId<DepartmentPrototype> CommandDepartment = "Command";
    private static readonly ProtoId<JobPrototype> ClownJob = "Clown";
    private static readonly ProtoId<JobPrototype> LibrarianJob = "Librarian";

    private const string HallucinationKey = "ADTHallucinations";
    private const string HallucinationPack = "SupermatterPack";

    private enum CommandCooldown : byte
    {
        Stun,
        Damage,
        Meme,
        None,
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTVoiceOfGodComponent, ADTVoiceOfGodActionEvent>(OnAction);
    }

    private void OnAction(Entity<ADTVoiceOfGodComponent> ent, ref ADTVoiceOfGodActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var user = args.Performer;

        if (!CanSpeak(ent, user))
            return;

        if (!_players.TryGetSessionByEntity(user, out var session))
            return;

        _quickDialog.OpenDialog(
            session,
            Loc.GetString("adt-voice-of-god-dialog-title"),
            Loc.GetString("adt-voice-of-god-dialog-prompt"),
            (string command) =>
            {
                if (TerminatingOrDeleted(ent) || TerminatingOrDeleted(user))
                    return;

                Speak(ent, user, command);
            });
    }

    private bool CanSpeak(Entity<ADTVoiceOfGodComponent> ent, EntityUid user)
    {
        var now = _timing.CurTime;

        if (now < ent.Comp.NextCommand)
        {
            var seconds = (int) Math.Ceiling((ent.Comp.NextCommand - now).TotalSeconds);
            _popup.PopupEntity(Loc.GetString("adt-voice-of-god-cooldown", ("seconds", seconds)), user, user);
            return false;
        }

        if (_mobState.IsIncapacitated(user))
            return false;

        if (_statusEffects.HasEffectComp<MutedStatusEffectComponent>(user))
        {
            _popup.PopupEntity(Loc.GetString("adt-voice-of-god-muted"), user, user);
            return false;
        }

        return true;
    }

    private void Speak(Entity<ADTVoiceOfGodComponent> ent, EntityUid user, string command)
    {
        command = command.Trim();

        if (command.Length == 0 || !CanSpeak(ent, user))
            return;

        _megafauna.SpeakLine(user, command.ToUpperInvariant(), "Default", 16, ent.Comp.SpeechColor, true);
        _audio.PlayPvs(ent.Comp.Sound, user);

        var message = command.ToLowerInvariant();
        var listeners = GetListeners(ent, user);

        if (listeners.Count == 0)
        {
            SetCooldown(ent, CommandCooldown.None);
            return;
        }

        var power = GetPowerMultiplier(ent, user);
        message = FocusListeners(listeners, message, ref power);

        var cooldown = Execute(ent, user, listeners, message, power);
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

    private float GetPowerMultiplier(Entity<ADTVoiceOfGodComponent> ent, EntityUid user)
    {
        var power = ent.Comp.BaseMultiplier;

        if (HasComp<BibleUserComponent>(user))
            power *= 2f;

        if (HasComp<MimePowersComponent>(user))
            power *= 0.5f;

        if (_mind.TryGetMind(user, out var mindId, out _) && _jobs.MindIsInDepartment(mindId, CommandDepartment))
            power *= 1.4f;

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

    private CommandCooldown Execute(Entity<ADTVoiceOfGodComponent> ent, EntityUid user, List<EntityUid> listeners, string message, float power)
    {
        if (StunWords.IsMatch(message))
        {
            foreach (var listener in listeners)
            {
                _stun.TryUpdateStunDuration(listener, TimeSpan.FromSeconds(6 * power));
            }

            return CommandCooldown.Stun;
        }

        if (WeakenWords.IsMatch(message))
        {
            foreach (var listener in listeners)
            {
                _stun.TryKnockdown(listener, TimeSpan.FromSeconds(6 * power));
            }

            return CommandCooldown.Stun;
        }

        if (SleepWords.IsMatch(message))
        {
            foreach (var listener in listeners)
            {
                _statusEffects.TryAddStatusEffectDuration(listener, SleepEffect, TimeSpan.FromSeconds(4 * power));
            }

            return CommandCooldown.Stun;
        }

        if (VomitWords.IsMatch(message))
        {
            foreach (var listener in listeners)
            {
                _vomit.Vomit(listener, force: true);
            }

            return CommandCooldown.Stun;
        }

        if (SilenceWords.IsMatch(message))
        {
            var silencePower = power;

            if (HasComp<MimePowersComponent>(user) || IsJob(user, LibrarianJob))
                silencePower *= 3f;

            foreach (var listener in listeners)
            {
                _statusEffects.TryAddStatusEffectDuration(listener, MutedEffect, TimeSpan.FromSeconds(20 * silencePower));
            }

            return CommandCooldown.Stun;
        }

        if (HallucinateWords.IsMatch(message))
        {
            foreach (var listener in listeners)
            {
                _hallucinations.StartHallucinations(listener, HallucinationKey, TimeSpan.FromSeconds(15 * power), true, HallucinationPack);
            }

            return CommandCooldown.Meme;
        }

        if (WakeUpWords.IsMatch(message))
        {
            foreach (var listener in listeners)
            {
                _statusEffects.TryRemoveStatusEffect(listener, SleepEffect);
                _sleeping.TryWaking(listener, true);
            }

            return CommandCooldown.Damage;
        }

        if (HealWords.IsMatch(message))
        {
            var heal = new DamageSpecifier(_proto.Index(BruteGroup), -10 * power);
            heal += new DamageSpecifier(_proto.Index(BurnGroup), -10 * power);

            foreach (var listener in listeners)
            {
                _damageable.TryChangeDamage(listener, heal, true, origin: user);
            }

            return CommandCooldown.Damage;
        }

        if (HurtWords.IsMatch(message))
        {
            var hurt = new DamageSpecifier(_proto.Index(HurtType), 15 * power);

            foreach (var listener in listeners)
            {
                _damageable.TryChangeDamage(listener, hurt, origin: user);
            }

            return CommandCooldown.Damage;
        }

        if (BleedWords.IsMatch(message))
        {
            foreach (var listener in listeners)
            {
                _bloodstream.TryModifyBleedAmount(listener, 5 * power);
            }

            return CommandCooldown.Damage;
        }

        if (BurnWords.IsMatch(message))
        {
            foreach (var listener in listeners)
            {
                if (!TryComp<FlammableComponent>(listener, out var flammable))
                    continue;

                _flammable.AdjustFireStacks(listener, power, flammable);
                _flammable.Ignite(listener, user, flammable, user);
            }

            return CommandCooldown.Damage;
        }

        if (RepulseWords.IsMatch(message))
        {
            var origin = _transform.GetMapCoordinates(user).Position;

            foreach (var listener in listeners)
            {
                var direction = _transform.GetMapCoordinates(listener).Position - origin;

                if (direction.LengthSquared() < 0.01f)
                    direction = _random.NextAngle().ToVec();

                _throwing.TryThrow(listener, Vector2.Normalize(direction) * 3 * power, 10f, user);
            }

            return CommandCooldown.Damage;
        }

        if (WhoAreYouWords.IsMatch(message))
        {
            foreach (var listener in listeners)
            {
                ForceSay(listener, Name(listener));
            }

            return CommandCooldown.Meme;
        }

        if (SayMyNameWords.IsMatch(message))
        {
            foreach (var listener in listeners)
            {
                ForceSay(listener, $"{Name(user)}!");
            }

            return CommandCooldown.Meme;
        }

        if (KnockKnockWords.IsMatch(message))
        {
            foreach (var listener in listeners)
            {
                ForceSay(listener, Loc.GetString("adt-voice-of-god-whos-there"));
            }

            return CommandCooldown.Meme;
        }

        if (StateLawsWords.IsMatch(message))
        {
            foreach (var listener in listeners)
            {
                if (!TryComp<SiliconLawBoundComponent>(listener, out var bound))
                    continue;

                foreach (var law in _laws.GetLaws(listener, bound).Laws.OrderBy(law => law.Order))
                {
                    ForceSay(listener, $"{law.Order}. {Loc.GetString(law.LawString)}");
                }
            }

            return CommandCooldown.Stun;
        }

        if (MoveWords.IsMatch(message))
        {
            foreach (var listener in listeners)
            {
                var step = _random.Pick(new[] { Vector2.UnitX, -Vector2.UnitX, Vector2.UnitY, -Vector2.UnitY });
                _throwing.TryThrow(listener, step, 4f, user, 0, null, false, false, false, false, false);
            }

            return CommandCooldown.Meme;
        }

        if (WalkWords.IsMatch(message) || RunWords.IsMatch(message))
            return CommandCooldown.Meme;

        if (HelpIntentWords.IsMatch(message))
        {
            foreach (var listener in listeners)
            {
                _combat.SetInCombatMode(listener, false);
            }

            return CommandCooldown.Meme;
        }

        if (HarmIntentWords.IsMatch(message))
        {
            foreach (var listener in listeners)
            {
                _combat.SetInCombatMode(listener, true);
            }

            return CommandCooldown.Meme;
        }

        if (FlipWords.IsMatch(message))
        {
            Emote(listeners, FlipEmote);
            return CommandCooldown.Meme;
        }

        if (RestWords.IsMatch(message))
        {
            foreach (var listener in listeners)
            {
                _stun.TryKnockdown(listener, TimeSpan.FromSeconds(2), autoStand: false, drop: false);
            }

            return CommandCooldown.Meme;
        }

        if (GetUpWords.IsMatch(message))
        {
            foreach (var listener in listeners)
            {
                _stun.TryUnstun(listener);
                _stun.TryUpdateParalyzeDuration(listener, TimeSpan.Zero);
                _stun.ForceStandUp(listener);
            }

            return CommandCooldown.Damage;
        }

        if (SitWords.IsMatch(message))
        {
            foreach (var listener in listeners)
            {
                foreach (var strap in _lookup.GetEntitiesInRange<StrapComponent>(Transform(listener).Coordinates, 0.4f))
                {
                    if (_buckle.TryBuckle(listener, listener, strap, popup: false))
                        break;
                }
            }

            return CommandCooldown.Meme;
        }

        if (StandWords.IsMatch(message))
        {
            foreach (var listener in listeners)
            {
                if (TryComp<BuckleComponent>(listener, out var buckle) && buckle.Buckled)
                    _buckle.TryUnbuckle((listener, buckle), listener, false);
            }

            return CommandCooldown.Meme;
        }

        if (JumpWords.IsMatch(message))
        {
            foreach (var listener in listeners)
            {
                ForceSay(listener, Loc.GetString("adt-voice-of-god-how-high"));
            }

            Emote(listeners, JumpEmote);
            return CommandCooldown.Meme;
        }

        if (SaluteWords.IsMatch(message))
        {
            Emote(listeners, SaluteEmote);
            return CommandCooldown.Meme;
        }

        if (DeathgaspWords.IsMatch(message))
        {
            Emote(listeners, DeathgaspEmote);
            return CommandCooldown.Meme;
        }

        if (ClapWords.IsMatch(message))
        {
            Emote(listeners, ClapEmote);
            return CommandCooldown.Meme;
        }

        if (HonkWords.IsMatch(message))
        {
            _audio.PlayPvs(ent.Comp.HonkSound, user);

            if (!IsJob(user, ClownJob))
                return CommandCooldown.Meme;

            foreach (var listener in listeners)
            {
                _stun.TryKnockdown(listener, TimeSpan.FromSeconds(14 * power));
            }

            return CommandCooldown.Stun;
        }

        if (MultispinWords.IsMatch(message))
        {
            Emote(listeners, SpinEmote);
            return CommandCooldown.Meme;
        }

        return CommandCooldown.None;
    }

    private void SetCooldown(Entity<ADTVoiceOfGodComponent> ent, CommandCooldown cooldown)
    {
        var delay = cooldown switch
        {
            CommandCooldown.Stun => ent.Comp.CooldownStun,
            CommandCooldown.Damage => ent.Comp.CooldownDamage,
            CommandCooldown.Meme => ent.Comp.CooldownMeme,
            _ => ent.Comp.CooldownNone,
        };

        ent.Comp.NextCommand = _timing.CurTime + delay;
    }

    private bool IsJob(EntityUid uid, ProtoId<JobPrototype> job)
    {
        return _mind.TryGetMind(uid, out var mindId, out _) && _jobs.MindHasJobWithId(mindId, job);
    }

    private void ForceSay(EntityUid uid, string message)
    {
        _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, false, ignoreActionBlocker: true);
    }

    private void Emote(List<EntityUid> listeners, ProtoId<EmotePrototype> emote)
    {
        foreach (var listener in listeners)
        {
            _chat.TryEmoteWithChat(listener, emote, ignoreActionBlocker: true, forceEmote: true);
        }
    }
}
