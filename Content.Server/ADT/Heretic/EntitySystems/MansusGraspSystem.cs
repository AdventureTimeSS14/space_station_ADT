//

using System.Linq;
using Content.Shared.ADT.Heretic.Common;
using Content.Server.Chat.Systems;
using Content.Server.Heretic.Abilities;
using Content.Server.Heretic.Components;
using Content.Server.Heretic.Components.PathSpecific;
using Content.Server.Popups;
using Content.Server.Speech.EntitySystems;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Actions;
using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Heretic;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Damage.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Timing;
using Content.Shared.Trigger;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Content.Shared.Mech.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Server.Heretic.EntitySystems;

public sealed class MansusGraspSystem : SharedMansusGraspSystem
{
    [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly RatvarianLanguageSystem _language = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly HereticAbilitySystem _ability = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly HereticSystem _heretic = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    public static readonly SoundSpecifier DefaultSound = new SoundPathSpecifier("/Audio/Items/welder.ogg");

    public static readonly LocId DefaultInvocation = "heretic-speech-mansusgrasp";

    public static readonly TimeSpan DefaultCooldown = TimeSpan.FromSeconds(10);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MansusGraspComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<MansusGraspComponent, MeleeHitEvent>(OnMelee);
        SubscribeLocalEvent<TagComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<RustGraspComponent, AfterInteractEvent>(OnRustInteract);
        SubscribeLocalEvent<DrawRitualRuneDoAfterEvent>(OnRitualRuneDoAfter);
        SubscribeLocalEvent<MansusGraspBlockTriggerComponent, AttemptTriggerEvent>(OnTriggerAttempt);
    }

    private void OnTriggerAttempt(Entity<MansusGraspBlockTriggerComponent> ent, ref AttemptTriggerEvent args)
    {
        if (HasComp<MansusGraspAffectedComponent>(args.User))
        {
            args.Cancelled = true;
            _popup.PopupEntity(Loc.GetString("mansus-grasp-trigger-fail"), args.User.Value, args.User.Value);
        }
        else if (HasComp<MansusGraspAffectedComponent>(Transform(ent).ParentUid))
            args.Cancelled = true;
    }

    private void OnRustInteract(EntityUid uid, RustGraspComponent comp, AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach || !_heretic.TryGetHereticComponent(args.User, out var heretic, out var mind) ||
            !TryComp(uid, out UseDelayComponent? delay) || _delay.IsDelayed((uid, delay), comp.Delay) ||
            !TryComp(uid, out MansusGraspComponent? grasp))
            return;

        if (args.Target is not { } target || _whitelist.IsWhitelistPass(grasp.Blacklist, target)) // ADT: no IsBlacklistPass, same semantics as IsWhitelistPass
        {
            RustTile();
            return;
        }

        // Death to catwalks
        if (_tag.HasTag(target, "Catwalk"))
        {
            args.Handled = true;
            InvokeGrasp(args.User, (uid, grasp));
            ResetDelay(comp.CatwalkDelayMultiplier);
            Del(args.Target);
            return;
        }

        if (!_ability.TryMakeRustWall(target, (mind, heretic)))
            return;

        args.Handled = true;
        InvokeGrasp(args.User, (uid, grasp));
        ResetDelay();

        return;

        void RustTile()
        {
            if (!args.ClickLocation.IsValid(EntityManager))
                return;

            if (!_mapManager.TryFindGridAt(_transform.ToMapCoordinates(args.ClickLocation), out var gridUid, out var mapGrid))
                return;

            var tileRef = _mapSystem.GetTileRef(gridUid, mapGrid, args.ClickLocation);
            var tileDef = (ContentTileDefinition) _tileDefinitionManager[tileRef.Tile.TypeId];

            if (!_ability.CanRustTile(tileDef))
                return;

            args.Handled = true;
            ResetDelay();
            InvokeGrasp(args.User, (uid, grasp));

            _ability.MakeRustTile(gridUid, mapGrid, tileRef, comp.TileRune);
        }

        void ResetDelay(float multiplier = 1f)
        {
            // Less delay the higher the path stage is
            var length = float.Lerp(comp.MaxUseDelay, comp.MinUseDelay, heretic.PathStage / 10f) * multiplier;
            _delay.SetLength((uid, delay), TimeSpan.FromSeconds(length), comp.Delay);
            _delay.TryResetDelay((uid, delay), false, comp.Delay);
        }
    }

    /// <summary>
    ///     ADT: the grasp used to fire at any entity and get wasted on random junk.
    ///     Valid targets are mobs, the transmutation rune, and whatever the current
    ///     path actually does something with (Blade blades, Lock doors, Rust structures/AI).
    /// </summary>
    private bool IsValidGraspTarget(EntityUid target, HereticComponent heretic)
    {
        if (HasComp<BatteryComponent>(target)
            || HasComp<PowerCellSlotComponent>(target)
            || HasComp<MechComponent>(target))
            return true;

        if (HasComp<MobStateComponent>(target) || HasComp<HereticRitualRuneComponent>(target))
            return true;

        return heretic.CurrentPath switch
        {
            // only its own blades, for the Mansus infusion
            "Blade" => _tag.HasTag(target, "HereticBladeBlade"),
            // knocks doors open
            "Lock" => HasComp<DoorComponent>(target),
            // rusts structures and kills station AI
            "Rust" => HasComp<StationAiHolderComponent>(target)
                      || _tag.HasAnyTag(target, "Wall", "Catwalk")
                      || HasComp<DamageableComponent>(target),
            _ => false,
        };
    }

    private bool GraspTarget(Entity<MansusGraspComponent> grasp, EntityUid user, EntityUid target)
    {
        if (!_heretic.TryGetHereticComponent(user, out var hereticComp, out _))
        {
            QueueDel(grasp);
            return true;
        }

        if (_whitelist.IsWhitelistPass(grasp.Comp.Blacklist, target)) // ADT: no IsBlacklistPass, same semantics as IsWhitelistPass
            return false;

        // ADT: reject nonsense targets instead of burning the grasp on them
        if (!IsValidGraspTarget(target, hereticComp))
        {
            _popup.PopupEntity(Loc.GetString("heretic-grasp-fail-invalid-target"), user, user);
            return false;
        }

        // ADT: the rune handles itself in HereticRitualSystem, grasp must survive and keep no cooldown
        if (HasComp<HereticRitualRuneComponent>(target))
            return false;

        var beforeEvent = new BeforeHarmfulActionEvent(user, HarmfulActionType.MansusGrasp);
        RaiseLocalEvent(target, beforeEvent);
        var cancelled = beforeEvent.Cancelled;
        if (!cancelled)
        {
            var ev = new BeforeCastTouchSpellEvent(target);
            RaiseLocalEvent(target, ev, true);
            cancelled = ev.Cancelled;
        }

        if (cancelled)
        {
            _actions.SetCooldown(hereticComp.MansusGraspAction, grasp.Comp.CooldownAfterUse);
            hereticComp.MansusGraspAction = EntityUid.Invalid;
            InvokeGrasp(user, grasp);
            QueueDel(grasp);
            return true;
        }

        TryDrainTarget(user, target);

        // upgraded grasp
        if (!TryApplyGraspEffectAndMark(user, hereticComp, target, grasp, out var triggerGrasp))
            return false;

        if (triggerGrasp && TryComp(target, out StatusEffectsComponent? status))
        {
            _stun.TryKnockdown(target, grasp.Comp.KnockdownTime, true);
            _stamina.TakeStaminaDamage(target, grasp.Comp.StaminaDamage);
            _language.DoRatvarian(target, grasp.Comp.SpeechTime, true, status);
            _statusEffect.TryAddStatusEffect<MansusGraspAffectedComponent>(target,
                "MansusGraspAffected",
                grasp.Comp.AffectedTime,
                true,
                status);
        }

        _actions.SetCooldown(hereticComp.MansusGraspAction, grasp.Comp.CooldownAfterUse);
        hereticComp.MansusGraspAction = EntityUid.Invalid;
        InvokeGrasp(user, grasp);
        QueueDel(grasp);
        return true;
    }

    private bool TryDrainTarget(EntityUid user, EntityUid target)
    {
        if (TryComp(target, out MechComponent? mech) && mech.MaxEnergy > 0)
        {
            mech.Energy = FixedPoint2.Max(FixedPoint2.Zero, mech.Energy - mech.MaxEnergy * 0.5f);
            _popup.PopupEntity(Loc.GetString("mansus-grasp-drain"), user, user);
            return true;
        }

        if (TryComp(target, out PowerCellSlotComponent? slot) &&
            _powerCell.TryGetBatteryFromSlotOrEntity((target, slot), out var cellBattery) &&
            cellBattery != null)
        {
            _battery.ChangeCharge(cellBattery.Value.Owner, -cellBattery.Value.Comp.MaxCharge * 0.5f);
            _popup.PopupEntity(Loc.GetString("mansus-grasp-drain"), user, user);
            return true;
        }

        if (TryComp(target, out BatteryComponent? battery))
        {
            _battery.ChangeCharge((target, battery), -battery.MaxCharge * 0.5f);
            _popup.PopupEntity(Loc.GetString("mansus-grasp-drain"), user, user);
            return true;
        }

        return false;
    }

    private void OnMelee(Entity<MansusGraspComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;
        // blocked from wide attacks in YAML. should never have more than 1
        if (args.HitEntities.Count > 1)
            return;
        var target = args.HitEntities.First();
        // no fumbling!
        if (target == args.User)
            return;
        args.Handled = GraspTarget(ent, args.User,target);
    }

    private void OnAfterInteract(Entity<MansusGraspComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;

        if (args.Target == null || args.Target == args.User)
            return;

        args.Handled = GraspTarget(ent, args.User,args.Target.Value);
    }

    public void InvokeGrasp(EntityUid user, Entity<MansusGraspComponent>? ent)
    {
        var (sound, invocation) = ent == null
            ? (DefaultSound, DefaultInvocation)
            : (ent.Value.Comp.Sound, ent.Value.Comp.Invocation);

        _audio.PlayPvs(sound, user);
        _chat.TrySendInGameICMessage(user, Loc.GetString(invocation), InGameICChatType.Speak, false);
    }

    private void OnAfterInteract(Entity<TagComponent> ent, ref AfterInteractEvent args)
    {
        var tags = ent.Comp.Tags;

        if (!args.CanReach
            || !args.ClickLocation.IsValid(EntityManager)
            || !_heretic.TryGetHereticComponent(args.User, out var heretic, out _) // not a heretic - how???
            || HasComp<ActiveDoAfterComponent>(args.User)) // prevent rune shittery
            return;

        var runeProto = "HereticRuneRitualDrawAnimation";
        float time = 14;

        if (TryComp(ent, out TransmutationRuneScriberComponent? scriber)) // if it is special rune scriber
        {
            runeProto = scriber.RuneDrawingEntity;
            time = scriber.Time;
        }
        else if (heretic.MansusGraspAction == EntityUid.Invalid // no grasp - not special
                 || !tags.Contains("Write") || !tags.Contains("Pen")) // not a pen
            return;

        args.Handled = true;

        // remove our rune if clicked
        if (args.Target != null && HasComp<HereticRitualRuneComponent>(args.Target))
        {
            // todo: add more fluff
            QueueDel(args.Target);
            return;
        }

        // spawn our rune
        var rune = Spawn(runeProto, args.ClickLocation);
        _transform.AttachToGridOrMap(rune);
        var dargs = new DoAfterArgs(EntityManager, args.User, time, new DrawRitualRuneDoAfterEvent(rune, args.ClickLocation), args.User)
        {
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            CancelDuplicate = false,
            MultiplyDelay = false,
            Broadcast = true,
        };
        _doAfter.TryStartDoAfter(dargs);
    }
    private void OnRitualRuneDoAfter(DrawRitualRuneDoAfterEvent ev)
    {
        // delete the animation rune regardless
        QueueDel(ev.RitualRune);

        if (!ev.Cancelled)
            _transform.AttachToGridOrMap(Spawn("HereticRuneRitual", ev.Coords));
    }
}
