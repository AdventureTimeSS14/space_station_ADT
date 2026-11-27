using Content.Server.ADT.Silicons.Borgs.Components;
using Content.Server.Silicons.Borgs;
using Content.Server.Silicons.Laws;
using Content.Server.Wires;
using Content.Shared.ADT.Silicons.Borgs;
using Content.Shared.ADT.Silicons.Borgs.Components;
using Content.Shared.Emag.Components;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.SurveillanceCamera;
using Content.Shared.SurveillanceCamera.Components;
using Content.Shared.Wires;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Silicons.Borgs;

public sealed partial class ADTBorgAiLinkWireAction : ComponentWireAction<ADTBorgAiLinkComponent>
{
    private SharedPopupSystem _popup => EntityManager.System<SharedPopupSystem>();
    private SharedMindSystem _mind => EntityManager.System<SharedMindSystem>();
    private AiRemoteControlSystem _remote => EntityManager.System<AiRemoteControlSystem>();

    public override string Name { get; set; } = "wire-name-borg-ai-link";
    public override Color Color { get; set; } = Color.Azure;
    public override bool LightRequiresPower { get; set; } = false;
    public override object StatusKey { get; } = "ADTBorgAiLink";

    public override StatusLightState? GetLightState(Wire wire, ADTBorgAiLinkComponent comp)
        => comp.AiLinkEnabled ? StatusLightState.On : StatusLightState.Off;

    public override bool Cut(EntityUid user, Wire wire, ADTBorgAiLinkComponent comp)
    {
        if (!comp.AiLinkEnabled)
            return true;

        comp.AiLinkEnabled = false;

        if (EntityManager.TryGetComponent(wire.Owner, out AiRemoteControllerComponent? controller) && controller.AiHolder != null)
            EntityManager.EventBus.RaiseLocalEvent(wire.Owner, new ReturnMindIntoAiEvent { Key = RemoteDevicesUiKey.Key }, true);

        _popup.PopupEntity(Loc.GetString("adt-borg-wires-popup-ai-cut"), wire.Owner, user);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, ADTBorgAiLinkComponent comp)
    {
        if (comp.AiLinkEnabled)
            return true;

        comp.AiLinkEnabled = true;
        _popup.PopupEntity(Loc.GetString("adt-borg-wires-popup-ai-restored"), wire.Owner, user);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, ADTBorgAiLinkComponent comp)
    {
        var borg = wire.Owner;

        if (!comp.AiLinkEnabled)
        {
            comp.AiLinkEnabled = true;
            _popup.PopupEntity(Loc.GetString("adt-borg-wires-popup-ai-restored"), borg, user);
            return;
        }

        if (!EntityManager.TryGetComponent(borg, out AiRemoteControllerComponent? controller))
        {
            _popup.PopupEntity(Loc.GetString("adt-borg-wires-popup-ai-none"), borg, user);
            return;
        }

        var current = controller.AiHolder;
        var candidates = new List<EntityUid>();
        var query = EntityManager.EntityQueryEnumerator<StationAiHeldComponent>();
        while (query.MoveNext(out var ai, out _))
        {
            if (_mind.TryGetMind(ai, out _, out _))
                candidates.Add(ai);
        }

        EntityUid? next = null;
        if (candidates.Count > 0)
        {
            if (current == null || !candidates.Contains(current.Value))
                next = candidates[0];
            else
                next = candidates[(candidates.IndexOf(current.Value) + 1) % candidates.Count];
        }

        if (next == null)
        {
            _popup.PopupEntity(Loc.GetString("adt-borg-wires-popup-ai-none"), borg, user);
            return;
        }

        if (next == current)
        {
            _popup.PopupEntity(Loc.GetString("adt-borg-wires-popup-ai-no-other"), borg, user);
            return;
        }

        if (current != null)
            EntityManager.EventBus.RaiseLocalEvent(borg, new ReturnMindIntoAiEvent { Key = RemoteDevicesUiKey.Key }, true);

        if (!_remote.AiTakeControl(next.Value, borg))
        {
            _popup.PopupEntity(Loc.GetString("adt-borg-wires-popup-ai-none"), borg, user);
            return;
        }

        var name = EntityManager.GetComponent<MetaDataComponent>(next.Value).EntityName;
        _popup.PopupEntity(Loc.GetString("adt-borg-wires-popup-ai-cycled", ("ai", name)), borg, user);
    }
}

public sealed partial class ADTBorgLawSyncWireAction : ComponentWireAction<ADTBorgAiLinkComponent>
{
    private IGameTiming _timing = default!;
    private SharedPopupSystem _popup => EntityManager.System<SharedPopupSystem>();

    public override string Name { get; set; } = "wire-name-borg-law-sync";
    public override Color Color { get; set; } = Color.YellowGreen;
    public override bool LightRequiresPower { get; set; } = false;
    public override object StatusKey { get; } = "ADTBorgLawSync";

    public override void Initialize()
    {
        base.Initialize();
        _timing = IoCManager.Resolve<IGameTiming>();
    }

    public override StatusLightState? GetLightState(Wire wire, ADTBorgAiLinkComponent comp)
    {
        if (comp.LawSyncFlashEnd > _timing.CurTime)
            return StatusLightState.BlinkingSlow;

        return comp.AiLinkEnabled && comp.LawSyncEnabled ? StatusLightState.On : StatusLightState.Off;
    }

    public override bool Cut(EntityUid user, Wire wire, ADTBorgAiLinkComponent comp)
    {
        if (!comp.LawSyncEnabled)
            return true;

        comp.LawSyncEnabled = false;

        _popup.PopupEntity(Loc.GetString("adt-borg-wires-popup-lawsync-cut"), wire.Owner, user);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, ADTBorgAiLinkComponent comp)
    {
        if (comp.LawSyncEnabled)
            return true;

        comp.LawSyncEnabled = true;

        if (!comp.AiLinkEnabled)
            comp.LawSyncFlashEnd = _timing.CurTime + TimeSpan.FromSeconds(1);

        _popup.PopupEntity(Loc.GetString("adt-borg-wires-popup-lawsync-mended"), wire.Owner, user);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, ADTBorgAiLinkComponent comp)
    {
    }
}

public sealed partial class ADTBorgCameraWireAction : ComponentWireAction<SurveillanceCameraComponent>
{
    private SharedSurveillanceCameraSystem _cameras => EntityManager.System<SharedSurveillanceCameraSystem>();

    public override string Name { get; set; } = "wire-name-borg-camera";
    public override Color Color { get; set; } = Color.LightGreen;
    public override bool LightRequiresPower { get; set; } = false;
    public override object StatusKey { get; } = "ADTBorgCamera";

    public override StatusLightState? GetLightState(Wire wire, SurveillanceCameraComponent comp)
        => comp.Active ? StatusLightState.On : StatusLightState.Off;

    public override bool Cut(EntityUid user, Wire wire, SurveillanceCameraComponent comp)
    {
        _cameras.SetActive(wire.Owner, false, comp);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, SurveillanceCameraComponent comp)
    {
        _cameras.SetActive(wire.Owner, true, comp);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, SurveillanceCameraComponent comp)
    {
        _cameras.SetActive(wire.Owner, true, comp);
    }
}

public sealed partial class ADTBorgResetModuleWireAction : ComponentWireAction<BorgSwitchableTypeComponent>
{
    public override string Name { get; set; } = "wire-name-borg-reset-module";
    public override Color Color { get; set; } = Color.Purple;
    public override bool LightRequiresPower { get; set; } = false;
    public override object StatusKey { get; } = "ADTBorgResetModule";

    public override StatusLightState? GetLightState(Wire wire, BorgSwitchableTypeComponent comp)
        => comp.SelectedBorgType != null ? StatusLightState.On : StatusLightState.Off;

    public override bool Cut(EntityUid user, Wire wire, BorgSwitchableTypeComponent comp)
    {
        return EntityManager.System<BorgSwitchableTypeSystem>().TryResetBorgType((wire.Owner, comp), user);
    }

    public override bool Mend(EntityUid user, Wire wire, BorgSwitchableTypeComponent comp)
    {
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, BorgSwitchableTypeComponent comp)
    {
    }
}

public sealed partial class ADTBorgResetLawsWireAction : BaseWireAction
{
    private SiliconLawSystem _laws => EntityManager.System<SiliconLawSystem>();
    private SharedPopupSystem _popup => EntityManager.System<SharedPopupSystem>();

    public override string Name { get; set; } = "wire-name-borg-reset-laws";
    public override Color Color { get; set; } = Color.Orange;
    public override bool LightRequiresPower { get; set; } = false;
    public override object StatusKey { get; } = "ADTBorgResetLaws";

    public override StatusLightState? GetLightState(Wire wire)
    {
        if (EntityManager.TryGetComponent(wire.Owner, out BorgChassisComponent? chassis)
            && chassis.BrainEntity is { } brain
            && EntityManager.TryGetComponent(brain, out SiliconLawProviderComponent? provider))
            return provider.Subverted ? StatusLightState.BlinkingFast : StatusLightState.Off;

        return StatusLightState.Off;
    }

    public override bool Cut(EntityUid user, Wire wire)
    {
        return false;
    }

    public override bool Mend(EntityUid user, Wire wire)
    {
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire)
    {
        if (!EntityManager.TryGetComponent(wire.Owner, out BorgChassisComponent? chassis) || chassis.BrainEntity is not { } brain)
            return;

        _laws.TryResetLaws(brain);

        EntityManager.RemoveComponent<EmaggedComponent>(wire.Owner);

        _popup.PopupEntity(Loc.GetString("adt-borg-wires-popup-laws-reset"), wire.Owner, user);
    }
}
