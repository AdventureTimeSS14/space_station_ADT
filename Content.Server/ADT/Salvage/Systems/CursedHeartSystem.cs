using Content.Server.Actions;
using Content.Server.ADT.Salvage.Components;
using Content.Server.Body.Systems;
using Content.Server.Popups;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.ADT.Silicon;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.SSDIndicator;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Salvage.Systems;

public sealed class CursedHeartSystem : EntitySystem
{
    [Dependency] private ActionsSystem _actions = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private MobThresholdSystem _mobThreshold = default!;
    [Dependency] private BodySystem _body = default!;

    private static readonly ProtoId<OrganCategoryPrototype> HeartCategory = "Heart";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CursedHeartComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CursedHeartComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CursedHeartComponent, PumpHeartActionEvent>(OnPump);
        SubscribeLocalEvent<CursedHeartComponent, ToggleHeartActionEvent>(OnToggle);

        SubscribeLocalEvent<CursedHeartOrganComponent, UseInHandEvent>(OnUseInHand);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CursedHeartComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_mobState.IsDead(uid))
                continue;

            if (comp.IsStopped)
                continue;

            if (TryComp<SSDIndicatorComponent>(uid, out var ssd) && ssd.IsSSD)
                continue;

            if (!TryComp<MindContainerComponent>(uid, out var mindContainer) || mindContainer.Mind == null)
                continue;

            if (_timing.CurTime >= comp.LastPump + TimeSpan.FromSeconds(comp.MaxDelay))
            {
                Damage(uid);
                comp.LastPump = _timing.CurTime;
            }
        }
    }

    private void Damage(EntityUid uid)
    {
        _bloodstream.TryModifyBloodLevel(uid, -50);
        _popup.PopupEntity(Loc.GetString("popup-cursed-heart-damage"), uid, uid, PopupType.MediumCaution);
    }

    private void OnStartup(EntityUid uid, CursedHeartComponent comp, ComponentStartup args)
    {
        _actions.AddAction(uid, ref comp.PumpActionEntity, "ActionPumpCursedHeart");
        _actions.AddAction(uid, ref comp.ToggleActionEntity, "ActionToggleCursedHeart");
        comp.LastPump = _timing.CurTime;

        if (!TryComp<BloodstreamComponent>(uid, out var bloodstream))
            return;

        comp.OriginalBloodReagents = new Solution(bloodstream.BloodReferenceSolution);
        _bloodstream.TryModifyBloodLevel(uid, -999);
        _bloodstream.ChangeBloodReagents(uid, new Solution("ADTCursedBlood", 200));
        _bloodstream.TryModifyBloodLevel(uid, 300);
    }

    private void OnShutdown(EntityUid uid, CursedHeartComponent comp, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, comp.PumpActionEntity);
        _actions.RemoveAction(uid, comp.ToggleActionEntity);

        if (TerminatingOrDeleted(uid))
            return;

        if (TryComp<MobThresholdsComponent>(uid, out var thresholds))
        {
            RestoreThresholds(uid, comp, thresholds);
            _mobThreshold.VerifyThresholds(uid, thresholds);
        }

        if (comp.OriginalBloodReagents != null)
            _bloodstream.ChangeBloodReagents(uid, comp.OriginalBloodReagents);
    }

    private void RestoreThresholds(EntityUid uid, CursedHeartComponent comp, MobThresholdsComponent thresholds)
    {
        if (comp.OriginalCritThreshold.HasValue)
        {
            _mobThreshold.SetMobStateThreshold(uid, comp.OriginalCritThreshold.Value, MobState.Critical, thresholds);
            comp.OriginalCritThreshold = null;
        }

        if (comp.OriginalSoftCritThreshold.HasValue)
        {
            _mobThreshold.SetMobStateThreshold(uid, comp.OriginalSoftCritThreshold.Value, MobState.SoftCritical, thresholds);
            comp.OriginalSoftCritThreshold = null;
        }
    }

    private void OnPump(EntityUid uid, CursedHeartComponent comp, PumpHeartActionEvent args)
    {
        if (args.Handled)
            return;
        if (comp.IsStopped || _mobState.IsDead(uid))
            return;
        args.Handled = true;
        _audio.PlayGlobal(new SoundPathSpecifier("/Audio/ADT/Heretic/heartbeat.ogg"), uid);
        _damage.TryChangeDamage(uid, new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Brute"), -3), true, false);
        _damage.TryChangeDamage(uid, new DamageSpecifier(_proto.Index<DamageGroupPrototype>("Burn"), -4), true, false);
        _bloodstream.TryModifyBloodLevel(uid, 17);
        comp.LastPump = _timing.CurTime;
    }

    private void OnToggle(EntityUid uid, CursedHeartComponent comp, ToggleHeartActionEvent args)
    {
        if (args.Handled)
            return;
        if (_mobState.IsDead(uid))
            return;
        args.Handled = true;

        if (!TryComp<MobThresholdsComponent>(uid, out var thresholds))
            return;

        if (comp.IsStopped)
        {
            // === ЗАПУСК СЕРДЦА ===
            comp.IsStopped = false;

            RestoreThresholds(uid, comp, thresholds);

            _popup.PopupEntity(Loc.GetString("popup-cursed-heart-start"), uid, uid, PopupType.Large);
            _audio.PlayGlobal(new SoundPathSpecifier("/Audio/ADT/Heretic/heartbeat.ogg"), uid);
        }
        else
        {
            // === ОСТАНОВКА СЕРДЦА ===
            comp.IsStopped = true;

            if (!comp.OriginalCritThreshold.HasValue &&
                _mobThreshold.TryGetThresholdForState(uid, MobState.Critical, out var currentCrit, thresholds))
            {
                comp.OriginalCritThreshold = currentCrit;
            }
            _mobThreshold.SetMobStateThreshold(uid, FixedPoint2.New(60), MobState.Critical, thresholds);

            if (!comp.OriginalSoftCritThreshold.HasValue &&
                comp.OriginalCritThreshold.HasValue &&
                _mobThreshold.TryGetThresholdForState(uid, MobState.SoftCritical, out var currentSoftCrit, thresholds))
            {
                comp.OriginalSoftCritThreshold = currentSoftCrit;
                var softCrit = currentSoftCrit.Value * FixedPoint2.New(60) / comp.OriginalCritThreshold.Value;
                _mobThreshold.SetMobStateThreshold(uid, softCrit, MobState.SoftCritical, thresholds);
            }

            _popup.PopupEntity(Loc.GetString("popup-cursed-heart-stop"), uid, uid, PopupType.LargeCaution);
            _audio.PlayGlobal(new SoundPathSpecifier("/Audio/ADT/Heretic/heartbeat.ogg"), uid);
        }

        _mobThreshold.VerifyThresholds(uid, thresholds);
    }

    private void OnUseInHand(EntityUid uid, CursedHeartOrganComponent comp, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<BloodstreamComponent>(args.User) || !HasComp<HumanoidProfileComponent>(args.User) || HasComp<MobIpcComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("popup-cursed-heart-bloodstream"), args.User, args.User, PopupType.Medium);
            return;
        }
        if (HasComp<CursedHeartComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("popup-cursed-heart-already-cursed"), args.User, args.User, PopupType.Medium);
            return;
        }
        if (!TryComp<BodyComponent>(args.User, out var body) || body.Organs == null)
            return;

        args.Handled = true;

        var hearts = new List<EntityUid>();
        foreach (var organ in body.Organs.ContainedEntities)
        {
            if (TryComp<OrganComponent>(organ, out var organComp) && organComp.Category == HeartCategory)
                hearts.Add(organ);
        }

        foreach (var heart in hearts)
        {
            _body.TryRemoveOrgan(heart);
        }

        if (!_body.TryInsertOrgan(args.User, uid))
            return;

        _popup.PopupEntity(Loc.GetString("popup-cursed-heart-use"), args.User, args.User, PopupType.LargeCaution);
        _damage.TryChangeDamage(args.User, new DamageSpecifier(_proto.Index<DamageTypePrototype>("Piercing"), 20), true, false);
        _audio.PlayGlobal(new SoundPathSpecifier("/Audio/ADT/Heretic/heartbeat.ogg"), args.User);
    }
}
