using Content.Server.Actions;
using Content.Shared.Actions;
using Content.Shared.Physics;
using Robust.Shared.Physics;
using Content.Shared.Poltergeist;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Content.Shared.Movement.Events;
using Content.Server.Power.EntitySystems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Random;
using Content.Shared.IdentityManagement;
using Content.Shared.Chat;
using System.Linq;
using Robust.Shared.Serialization.Manager;
using Content.Shared.Alert;
using Robust.Server.GameObjects;
using Content.Server.Chat.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.StatusEffect;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Humanoid;
using Robust.Shared.Containers;
using Content.Shared.DoAfter;
using System.Numerics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Server.Chat.Managers;
using Robust.Shared.Prototypes;
using Content.Shared.Stunnable;
using Content.Server.Power.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Eye;
using Content.Server.Light.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.Chat;
using Content.Shared.PowerCell.Components;
using Robust.Shared.Timing;
using Content.Shared.Inventory;
using Content.Shared.Interaction.Components;
using Content.Shared.Mindshield.Components;
using Content.Server.Body.Systems;
using Content.Shared.GhostInteractions;
using Content.Shared.Bible.Components;
using Content.Server.EUI;
using Content.Shared.Throwing;
using Content.Shared.Item;
using Robust.Shared.Physics.Components;
using Content.Shared.Mobs;

namespace Content.Server.Poltergeist;

public sealed partial class PoltergeistSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedPhysicsSystem _physicsSystem = default!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly ApcSystem _apcSystem = default!;
    [Dependency] protected readonly IGameTiming GameTiming = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedStunSystem _sharedStun = default!;
    [Dependency] private readonly EuiManager _euiManager = null!;
    [Dependency] private readonly BatterySystem _batterySystem = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PoltergeistComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PoltergeistComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<PoltergeistComponent, PoltergeistMalfunctionActionEvent>(OnMalf);
        SubscribeLocalEvent<PoltergeistComponent, PoltergeistNoisyActionEvent>(OnNoisy);
        SubscribeLocalEvent<PoltergeistComponent, PoltergeistRestInPeaceActionEvent>(OnRestInPeace);

        SubscribeLocalEvent<PoltergeistComponent, AlternativeSpeechEvent>(OnTrySpeak);

        SubscribeLocalEvent<PotentialPoltergeistComponent, MobStateChangedEvent>(OnMobState);
    }
    private void OnMobState(EntityUid uid, PotentialPoltergeistComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead);
        {
            if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            {
                var poltergei = Spawn("ADTMobPoltergeist", Transform(uid).Coordinates);
                _mindSystem.TransferTo(mindId, poltergei);
            }
        }
    }

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

        if (HasComp<StatusEffectsComponent>(target) && HasComp<BatteryComponent>(target)) //&& _status.TryAddStatusEffect<SeeingStaticComponent>(target, "SeeingStatic", timeStatic, false))
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
                _chat.TrySendInGameICMessage(ent, args.Message, InGameICChatType.Whisper, false);
        }
        // TODO ghost radio
        // upd: it took 5 minutes to do it WHAT DA HEEEEEL
    }

    private void OnRestInPeace(EntityUid uid, PoltergeistComponent _, PoltergeistRestInPeaceActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!_mindSystem.TryGetMind(uid, out var mindId, out var mind) || mind.Session == null)
            return;

        _euiManager.OpenEui(new RestInPeaceEui(uid, this), mind.Session);
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
