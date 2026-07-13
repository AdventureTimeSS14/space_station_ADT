using Content.Server.Silicons.Laws;
using Content.Shared.Radio.Components;
using Content.Shared.ADT.Silicons.Borgs;
using Content.Shared.ADT.Silicons.Borgs.Components;
using Content.Shared.Actions;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Content.Shared.Verbs;
using Content.Shared.Destructible;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Content.Shared.Damage.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Containers;

namespace Content.Server.ADT.Silicons.Borgs;

public sealed class AiRemoteControlSystem : SharedAiRemoteControlSystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SiliconLawSystem _lawSystem = default!;
    [Dependency] private readonly SharedStationAiSystem _stationAiSystem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedTransformSystem _xformSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AiRemoteControllerComponent, ReturnMindIntoAiEvent>(OnReturnMindIntoAi);
        SubscribeLocalEvent<AiRemoteControllerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AiRemoteControllerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<AiRemoteControllerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<AiRemoteControllerComponent, DestructionEventArgs>(OnBorgDestroyed);
        SubscribeLocalEvent<AiRemoteControllerComponent, MobStateChangedEvent>(OnBorgMobStateChanged);
        SubscribeLocalEvent<StationAiBrainComponent, MobStateChangedEvent>(OnAiMobStateChanged);
        SubscribeLocalEvent<StationAiHeldComponent, RemoteDeviceActionMessage>(OnUiRemoteAction);
        SubscribeLocalEvent<StationAiHeldComponent, ToggleRemoteDevicesScreenEvent>(OnToggleRemoteDevicesScreen);
    }

    private void OnMapInit(Entity<AiRemoteControllerComponent> entity, ref MapInitEvent args)
    {
        EnsureComp<StationAiVisionComponent>(entity.Owner);
        EntityUid? actionEnt = null;

        _actions.AddAction(entity.Owner, ref actionEnt, entity.Comp.BackToAiAction);

        if (actionEnt != null)
            entity.Comp.BackToAiActionEntity = actionEnt.Value;
    }

    private void OnShutdown(Entity<AiRemoteControllerComponent> entity, ref ComponentShutdown args)
    {
        _actions.RemoveAction(entity.Owner, entity.Comp.BackToAiActionEntity);

        if (TryComp(entity, out IntrinsicRadioTransmitterComponent? transmitter) && entity.Comp.PreviouslyTransmitterChannels != null)
            transmitter.Channels = [.. entity.Comp.PreviouslyTransmitterChannels, ];

        if (TryComp(entity, out ActiveRadioComponent? activeRadio) && entity.Comp.PreviouslyActiveRadioChannels != null)
            activeRadio.Channels = [.. entity.Comp.PreviouslyActiveRadioChannels, ];

        var returnEvent = new ReturnMindIntoAiEvent { Key = RemoteDevicesUiKey.Key };
        RaiseLocalEvent(entity.Owner, returnEvent, true);
    }

    private void OnGetVerbs(Entity<AiRemoteControllerComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;

        if (!HasComp<StationAiHeldComponent>(user))
            return;

        var verb = new AlternativeVerb
        {
            Text = Loc.GetString("ai-remote-control"),
            Act = () =>
            {
                AiTakeControl(user, entity);
            }
        };
        args.Verbs.Add(verb);
    }

    private void OnReturnMindIntoAi(Entity<AiRemoteControllerComponent> entity, ref ReturnMindIntoAiEvent args)
    {
        if (entity.Comp.AiHolder.HasValue)
            RewriteLaws(entity.Owner, entity.Comp.AiHolder.Value);

        ReturnMindIntoAi(entity.Owner);
    }

    public void AiTakeControl(EntityUid ai, EntityUid entity)
    {
        if (!TryComp<StationAiHeldComponent>(ai, out var stationAiHeldComp))
            return;

        if (!TryComp<AiRemoteControllerComponent>(entity, out var aiRemoteComp))
            return;

        if (_mobState.IsIncapacitated(entity))
            return;

        if (!_stationAiSystem.TryGetCore(ai, out var stationAiCore) ||
            !_stationAiSystem.TryGetHeld(stationAiCore, out var aiBrain) ||
            aiBrain == null)
        {
            return;
        }

        if (!TryComp<MetaDataComponent>(aiBrain.Value, out var aiMeta))
        {
            return;
        }

        if (!_mind.TryGetMind(aiBrain.Value, out var mindId, out _))
        {
            return;
        }

        if (TryComp(entity, out IntrinsicRadioTransmitterComponent? transmitter))
        {
            aiRemoteComp.PreviouslyTransmitterChannels = [.. transmitter.Channels, ];
        }

        if (TryComp(entity, out ActiveRadioComponent? activeRadio))
        {
            aiRemoteComp.PreviouslyActiveRadioChannels = [.. activeRadio.Channels, ];
        }

        if (TryComp(aiBrain.Value, out IntrinsicRadioTransmitterComponent? stationAiTransmitter) && transmitter != null)
            transmitter.Channels = [.. stationAiTransmitter.Channels, ];

        if (TryComp(aiBrain.Value, out ActiveRadioComponent? stationAiActiveRadio) && activeRadio != null)
            activeRadio.Channels = [.. stationAiActiveRadio.Channels, ];

        _mind.ControlMob(aiBrain.Value, entity);
        aiRemoteComp.AiHolder = aiBrain.Value;
        aiRemoteComp.LinkedMind = mindId;

        _metaData.SetEntityName(entity, aiMeta.EntityName);

        stationAiHeldComp.CurrentConnectedEntity = entity;

        _stationAiSystem.SwitchRemoteEntityMode(stationAiCore!, false);

        RewriteLaws(aiBrain.Value, entity);
    }

    private void OnBorgDestroyed(Entity<AiRemoteControllerComponent> entity, ref DestructionEventArgs args)
    {
        if (entity.Comp.AiHolder.HasValue)
        {
            var returnEvent = new ReturnMindIntoAiEvent { Key = RemoteDevicesUiKey.Key };
            RaiseLocalEvent(entity.Owner, returnEvent, true);
        }
    }

    private void OnBorgMobStateChanged(Entity<AiRemoteControllerComponent> entity, ref MobStateChangedEvent args)
    {
        if (entity.Comp.AiHolder.HasValue && _mobState.IsIncapacitated(entity.Owner, args.Component))
        {
            var returnEvent = new ReturnMindIntoAiEvent { Key = RemoteDevicesUiKey.Key };
            RaiseLocalEvent(entity.Owner, returnEvent, true);
        }
    }

    private void OnAiMobStateChanged(Entity<StationAiBrainComponent> ai, ref MobStateChangedEvent args)
    {
        if (!_mobState.IsIncapacitated(ai.Owner, args.Component))
            return;

        if (!TryComp<StationAiHeldComponent>(ai.Owner, out var heldComp) || heldComp.CurrentConnectedEntity == null)
            return;

        var borg = heldComp.CurrentConnectedEntity.Value;
        if (!TryComp<AiRemoteControllerComponent>(borg, out _))
            return;

        RaiseLocalEvent(borg, new ReturnMindIntoAiEvent { Key = RemoteDevicesUiKey.Key }, true);
    }

    private void OnToggleRemoteDevicesScreen(EntityUid uid, StationAiHeldComponent component, ToggleRemoteDevicesScreenEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(uid, out var actor))
            return;
        args.Handled = true;

        _userInterface.TryToggleUi(uid, RemoteDevicesUiKey.Key, actor.PlayerSession);

        var query = EntityManager.EntityQueryEnumerator<AiRemoteControllerComponent>();

        var remoteDevices = new List<RemoteDevicesData>();

        while (query.MoveNext(out var queryUid, out _))
        {
            var meta = Comp<MetaDataComponent>(queryUid);
            SpriteSpecifier? spriteSpecifier = null;

            if (TryComp<BorgTransponderComponent>(queryUid, out var transponder))
            {
                spriteSpecifier = transponder.Sprite;
            }

            if (spriteSpecifier == null)
            {
                spriteSpecifier = new SpriteSpecifier.EntityPrototype(meta.EntityPrototype?.ID ?? "BorgChassisGeneric");
            }

            var isIncapacitated = _mobState.IsIncapacitated(queryUid);

            var data = new RemoteDevicesData
            {
                NetEntityUid = GetNetEntity(queryUid),
                DisplayName = meta.EntityName,
                Sprite = spriteSpecifier,
                IsIncapacitated = isIncapacitated
            };

            remoteDevices.Add(data);
        }

        var state = new RemoteDevicesBuiState(remoteDevices);

        _userInterface.SetUiState(uid, RemoteDevicesUiKey.Key, state);
    }

    private void OnUiRemoteAction(EntityUid uid, StationAiHeldComponent component, RemoteDeviceActionMessage msg)
    {
        if (msg.RemoteAction == null)
            return;

        var target = GetEntity(msg.RemoteAction?.Target);

        if (!HasComp<AiRemoteControllerComponent>(target))
            return;

        if (msg.RemoteAction?.ActionType == RemoteDeviceActionType.MoveToDevice)
        {
            if (!_stationAiSystem.TryGetCore(uid, out var stationAiCore) || stationAiCore.Comp?.RemoteEntity == null)
                return;
            _xformSystem.SetCoordinates(stationAiCore.Comp.RemoteEntity.Value, Transform(target.Value).Coordinates);
        }

        if (msg.RemoteAction?.ActionType == RemoteDeviceActionType.TakeControl)
            AiTakeControl(uid, target.Value);
    }

    public void RewriteLaws(EntityUid from, EntityUid to)
    {
        if (!TryComp<SiliconLawProviderComponent>(from, out var fromLawsComp))
            return;

        if (!HasComp<SiliconLawProviderComponent>(to))
            return;

        if (fromLawsComp.Lawset == null)
            return;

        var fromLaws = _lawSystem.GetLaws(from);

        _lawSystem.SetLawsSilent(fromLaws.Laws, to);
    }
}