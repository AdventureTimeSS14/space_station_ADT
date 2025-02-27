using Content.Shared.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Content.Shared.Tag;
using Content.Shared.Popups;
using Content.Shared.ADT.BoomBox;
using Content.Shared.UserInterface;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Emag.Systems;
using Content.Server.Speech.Components;
using Content.Server.Radio.Components;
using Content.Shared.ADT.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Server.BoomBox;

public sealed partial class BoomBoxSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BoomBoxComponent, EntInsertedIntoContainerMessage>(OnItemInserted);
        SubscribeLocalEvent<BoomBoxComponent, EntRemovedFromContainerMessage>(OnItemRemoved);
        SubscribeLocalEvent<BoomBoxComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<BoomBoxComponent, GotEmaggedEvent>(OnEmagged);
        SubscribeLocalEvent<BoomBoxComponent, AfterActivatableUIOpenEvent>(OnToggleInterface);
        SubscribeLocalEvent<BoomBoxComponent, BoomBoxPlusVolMessage>(OnPlusVolButtonPressed);
        SubscribeLocalEvent<BoomBoxComponent, BoomBoxMinusVolMessage>(OnMinusVolButtonPressed);
        SubscribeLocalEvent<BoomBoxComponent, BoomBoxStartMessage>(OnStartButtonPressed);
        SubscribeLocalEvent<BoomBoxComponent, BoomBoxStopMessage>(OnStopButtonPressed);
    }

    private void OnItemInserted(EntityUid uid, BoomBoxComponent comp, EntInsertedIntoContainerMessage args)
    {
        _popup.PopupEntity(Loc.GetString("tape-in"), uid);

        comp.Inserted = true;

        if (!TryComp<BoomBoxTapeComponent>(args.Entity, out var tapeComp) || tapeComp.SoundPath == null)
            return;

        comp.SoundPath = tapeComp.SoundPath;
    }

    private void OnItemRemoved(EntityUid uid, BoomBoxComponent comp, EntRemovedFromContainerMessage args)
    {
        _popup.PopupEntity(Loc.GetString("tape-out"), uid);

        StopPlay(uid, comp);

        comp.Inserted = false;
        comp.Enabled = false;
    }

    private void OnMinusVolButtonPressed(EntityUid uid, BoomBoxComponent component, BoomBoxMinusVolMessage args)
    {
        MinusVol(uid, component);
    }

    private void OnPlusVolButtonPressed(EntityUid uid, BoomBoxComponent component, BoomBoxPlusVolMessage args)
    {
        PlusVol(uid, component);
    }

    private void OnStartButtonPressed(EntityUid uid, BoomBoxComponent component, BoomBoxStartMessage args)
    {
        StartPlay(uid, component);
    }

    private void OnStopButtonPressed(EntityUid uid, BoomBoxComponent component, BoomBoxStopMessage args)
    {
        StopPlay(uid, component);
    }

    private void OnToggleInterface(EntityUid uid, BoomBoxComponent component, AfterActivatableUIOpenEvent args)
    {
        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, BoomBoxComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        bool canPlusVol = true;
        bool canMinusVol = true;
        bool canStop = false;
        bool canStart = false;

        if (component.Volume == 5)
            canPlusVol = false;

        if (component.Volume == -13)
            canMinusVol = false;

        if (component.Inserted)
        {
            if (component.Enabled)
            {
                canStart = false;
                canStop = true;
            }
            else
            {
                canStart = true;
                canStop = false;
            }
        }
        else
        {
            canStart = false;
            canStop = false;
        }


        var state = new BoomBoxUiState(canPlusVol, canMinusVol, canStop, canStart);
        _userInterface.SetUiState(uid, BoomBoxUiKey.Key, state);
    }

    private void MinusVol(EntityUid uid, BoomBoxComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Volume = component.Volume - 3f;
        _audioSystem.SetVolume(component.Stream, component.Volume);

        UpdateUserInterface(uid, component);
    }

    private void PlusVol(EntityUid uid, BoomBoxComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Volume = component.Volume + 3f;
        _audioSystem.SetVolume(component.Stream, component.Volume);

        UpdateUserInterface(uid, component);
    }

    private void StartPlay(EntityUid uid, BoomBoxComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Inserted && !component.Enabled)
        {
            component.Enabled = true;
            _popup.PopupEntity(Loc.GetString("boombox-on"), uid);

            // Отправляем клиентам сообщение о начале проигрывания
            var msg = new BoomBoxPlayMessage(component.SoundPath, component.Volume);
            RaiseNetworkEvent(msg, uid); // Отправляем всем, кто находится рядом
        }

        UpdateUserInterface(uid, component);
    }

    private void StopPlay(EntityUid uid, BoomBoxComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.Inserted && component.Enabled)
        {
            component.Enabled = false;

            _popup.PopupEntity(Loc.GetString("boombox-off"), uid);

            // Отправляем клиентам команду остановить звук
            RaiseNetworkEvent(new BoomBoxStopClientMessage(), uid);
        }

        UpdateUserInterface(uid, component);
    }

    private void OnInteractUsing(EntityUid uid, BoomBoxComponent component, InteractUsingEvent args)
    {
        component.User = args.User;
    }

    public void OnEmagged(EntityUid uid, BoomBoxComponent component, ref GotEmaggedEvent args)
    {
        var comp = EnsureComp<RadioMicrophoneComponent>(uid);
        comp.Enabled = true;
        comp.BroadcastChannel = "Syndicate";
        comp.ToggleOnInteract = false;
        comp.ListenRange = 4;

        var comp2 = EnsureComp<ActiveListenerComponent>(uid);
        comp2.Range = 4;

        _popup.PopupEntity(Loc.GetString("boombox-emagged"), uid);
        _audioSystem.PlayPvs(component.EmagSound, uid);
        args.Handled = true;
    }
}
