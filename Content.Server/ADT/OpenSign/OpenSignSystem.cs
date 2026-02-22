using Content.Server.DeviceLinking.Systems;
using Content.Shared.ADT.OpenSign;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.Interaction;
using Content.Shared.Power;
using Content.Server.Power.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.ADT.OpenSign;

public sealed class OpenSignSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiver = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OpenSignComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<OpenSignComponent, ActivateInWorldEvent>(OnActivated);
        SubscribeLocalEvent<OpenSignComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<OpenSignComponent, PowerChangedEvent>(OnPowerChanged);
    }

    private void OnInit(EntityUid uid, OpenSignComponent comp, ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(uid, comp.OnPort, comp.OffPort, comp.TogglePort);
        _deviceLink.EnsureSourcePorts(uid, comp.StatusPort);
    }

    private void OnActivated(EntityUid uid, OpenSignComponent comp, ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        if (!comp.State && !_powerReceiver.IsPowered(uid))
            return;

        SetState(uid, comp, !comp.State);
        args.Handled = true;
    }

    private void OnSignalReceived(EntityUid uid, OpenSignComponent comp, ref SignalReceivedEvent args)
    {
        var state = SignalState.Momentary;
        args.Data?.TryGetValue(DeviceNetworkConstants.LogicState, out state);

        if (args.Port == comp.TogglePort)
        {
            if (state == SignalState.High || state == SignalState.Momentary)
            {
                if (!comp.State && !_powerReceiver.IsPowered(uid))
                    return;

                SetState(uid, comp, !comp.State);
            }
        }
        else if (args.Port == comp.OnPort)
        {
            if (state == SignalState.High || state == SignalState.Momentary)
            {
                if (!_powerReceiver.IsPowered(uid))
                    return;

                SetState(uid, comp, true);
            }
        }
        else if (args.Port == comp.OffPort)
        {
            if (state == SignalState.High || state == SignalState.Momentary)
                SetState(uid, comp, false);
        }
    }

    private void OnPowerChanged(EntityUid uid, OpenSignComponent comp, ref PowerChangedEvent args)
    {
        if (args.Powered)
            return;

        if (comp.State)
            SetState(uid, comp, false);
    }

    private void SetState(EntityUid uid, OpenSignComponent comp, bool state)
    {
        comp.State = state;
        _appearance.SetData(uid, OpenSignVisuals.Key, state);
        _deviceLink.SendSignal(uid, comp.StatusPort, state);
        _audio.PlayPvs(comp.ClickSound, uid, AudioParams.Default.WithVariation(0.125f).WithVolume(8f));
    }
}
