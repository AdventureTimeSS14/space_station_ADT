using Content.Shared.Actions;
using Robust.Shared.Physics;
using Content.Shared.ADT.Poltergeist;
using Content.Shared.Popups;
using Content.Server.Power.EntitySystems;
using Robust.Shared.Random;
using Content.Shared.IdentityManagement;
using Content.Server.Chat.Systems;
using Content.Shared.StatusEffect;
using Content.Shared.Mind;
using Robust.Shared.Containers;
using Content.Shared.Stunnable;
using Content.Server.Power.Components;
using Content.Server.Chat;
using Robust.Shared.Timing;
using Content.Shared.ADT.GhostInteractions;
using Content.Shared.Bible.Components;
using Content.Server.EUI;
using Content.Shared.Throwing;
using Content.Shared.Item;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Content.Server.Singularity.Events;
using Content.Shared.ADT.Silicon.Components;

namespace Content.Server.ADT.Poltergeist;

public sealed partial class PoltergeistSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] private readonly EuiManager _euiManager = null!;
    [Dependency] private readonly BatterySystem _batterySystem = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PoltergeistComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PoltergeistComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<PoltergeistComponent, PoltergeistMalfunctionActionEvent>(OnMalf);
        SubscribeLocalEvent<PoltergeistComponent, PoltergeistNoisyActionEvent>(OnNoisy);
        SubscribeLocalEvent<PoltergeistComponent, PoltergeistRestInPeaceActionEvent>(OnRestInPeace);

        SubscribeLocalEvent<PoltergeistComponent, AlternativeSpeechEvent>(OnTrySpeak);
        SubscribeLocalEvent<PoltergeistComponent, EventHorizonAttemptConsumeEntityEvent>(OnSinguloConsumeAttempt);
        // SubscribeLocalEvent<PotentialPoltergeistComponent, MobStateChangedEvent>(OnMobState);
    }
    // private void OnMobState(EntityUid uid, PotentialPoltergeistComponent component, MobStateChangedEvent args)
    // {
    //     if (args.NewMobState == MobState.Dead);
    //     {
    //         if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
    //         {
    //             var poltergei = Spawn("ADTMobPoltergeist", Transform(uid).Coordinates);
    //             _mindSystem.TransferTo(mindId, poltergei);
    //         }
    //     }
    // }

    private void OnMapInit(EntityUid uid, PoltergeistComponent component, MapInitEvent args)
    {
        _action.AddAction(uid, ref component.MalfunctionActionEntity, component.MalfunctionAction);
        _action.AddAction(uid, ref component.NoisyActionEntity, component.NoisyAction);
        _action.AddAction(uid, ref component.DieActionEntity, component.DieAction);
    }

    private void OnShutdown(EntityUid uid, PoltergeistComponent component, ComponentShutdown args)
    {
        _action.RemoveAction(uid, component.MalfunctionActionEntity);
        _action.RemoveAction(uid, component.NoisyActionEntity);
        _action.RemoveAction(uid, component.DieActionEntity);
    }

    private void OnMalf(EntityUid uid, PoltergeistComponent component, PoltergeistMalfunctionActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var target = args.Target;

        var time = TimeSpan.FromSeconds(1);
        var timeStatic = TimeSpan.FromSeconds(2);

        if (HasComp<StatusEffectsComponent>(target) && HasComp<BatteryComponent>(target) && _status.TryAddStatusEffect<SeeingStaticComponent>(target, "SeeingStatic", timeStatic, false))
        {
            _status.TryAddStatusEffect<KnockedDownComponent>(target, "KnockedDown", time, false);
            _status.TryAddStatusEffect<StunnedComponent>(target, "Stun", time, false);
            _status.TryAddStatusEffect<SlowedDownComponent>(target, "SlowedDown", timeStatic, false);
        }

        if (TryComp<BatteryComponent>(target, out var batteryComp))
        {
            var charge = batteryComp.CurrentCharge * 0.75f;
            _batterySystem.SetCharge(target, charge, batteryComp);
        }

        if (TryComp<ContainerManagerComponent>(target, out var containerManagerComponent))
        {
            foreach (var container in containerManagerComponent.Containers.Values)
            {
                foreach (var entity in container.ContainedEntities)
                {
                    if (TryComp<BatteryComponent>(entity, out var entBatteryComp))
                    {
                        var newCharge = entBatteryComp.CurrentCharge * 0.75f;
                        _batterySystem.SetCharge(entity, newCharge, batteryComp);
                    }
                }
            }
        }

        var selfMessage = Loc.GetString("poltergeist-malf-self", ("target", Identity.Entity(target, EntityManager)));
        _popup.PopupEntity(selfMessage, uid, uid);
    }

    private void OnNoisy(EntityUid uid, PoltergeistComponent component, PoltergeistNoisyActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var target = args.Target;
        var strength = _random.NextFloat(5f, 12f);
        if (HasComp<ItemComponent>(target) && TryComp<PhysicsComponent>(target, out var phys) && phys.BodyType != BodyType.Static)
            _throwing.TryThrow(target, _random.NextAngle().ToWorldVec(), strength);
        else
            return;
    }

    private void OnTrySpeak(EntityUid uid, PoltergeistComponent component, AlternativeSpeechEvent args)
    {
        args.Cancel();

        var selfMessage = Loc.GetString("poltergeist-say");
        _popup.PopupEntity(selfMessage, uid, uid);

        foreach (var ent in _lookup.GetEntitiesInRange(Transform(uid).Coordinates, 8f))
        {
            if (TryComp<GhostRadioComponent>(ent, out var radio) && radio.Enabled)
                _chat.TrySendInGameICMessage(ent, args.Message, InGameICChatType.Whisper, false, ignoreActionBlocker: true);
        }
        // TODO ghost radio
        // upd: it took 5 minutes to do it WHAT DA HEEEEEL
    }

    private void OnRestInPeace(EntityUid uid, PoltergeistComponent _, PoltergeistRestInPeaceActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind) || !_player.TryGetSessionById(mind.UserId, out var session))
            return;

        _euiManager.OpenEui(new RestInPeaceEui(uid, this), session);
    }

    private void OnSinguloConsumeAttempt(EntityUid uid, PoltergeistComponent component, ref EventHorizonAttemptConsumeEntityEvent args)
    {
        args.Cancelled = true;
    }

    public void Rest(EntityUid uid)
    {
        QueueDel(uid);
        foreach (var chaplain in EntityQuery<ChaplainComponent>())
        {
            var message = Loc.GetString("poltergeist-death-chaplain");
            _popup.PopupEntity(message, chaplain.Owner, chaplain.Owner);
        }
    }
}
