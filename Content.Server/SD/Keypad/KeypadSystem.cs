using Content.Server.DeviceLinking.Systems;
using Content.Shared._SD.Keypad;
using Content.Shared.Hands.Components;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;

namespace Content.Server._SD.Keypad;

public sealed class KeypadSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KeypadComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<KeypadComponent, GetVerbsEvent<AlternativeVerb>>(AddChangeCodeVerb);

        SubscribeLocalEvent<KeypadComponent, KeypadKeypadPressedMessage>(OnKeypadPressed);
        SubscribeLocalEvent<KeypadComponent, KeypadClearButtonPressedMessage>(OnClearPressed);
        SubscribeLocalEvent<KeypadComponent, KeypadEnterButtonPressedMessage>(OnEnterPressed);
        SubscribeLocalEvent<KeypadComponent, KeypadCancelButtonPressedMessage>(OnCancelPressed);
    }

    private void OnInit(EntityUid uid, KeypadComponent component, ComponentInit args)
    {
        _deviceLink.EnsureSourcePorts(uid, component.UnlockedPort);
        UpdateUi(uid, component);
    }

    private void AddChangeCodeVerb(EntityUid uid, KeypadComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || component.State != KeypadState.Normal)
            return;

        if (!HasComp<HandsComponent>(args.User))
            return;

        AlternativeVerb verb = new()
        {
            Text = Loc.GetString("keypad-change-code-verb"),
            Act = () =>
            {
                StartChangeCode(uid, component, args.User);
            },
            Priority = 2
        };
        args.Verbs.Add(verb);
    }

    private void OnKeypadPressed(EntityUid uid, KeypadComponent component, KeypadKeypadPressedMessage args)
    {
        _audio.PlayPvs(component.KeypadPressSound, uid);

        if (component.CurrentCode.Length >= component.MaxCodeLength)
            return;

        component.CurrentCode += args.Button;
        UpdateUi(uid, component);
    }

    private void OnClearPressed(EntityUid uid, KeypadComponent component, KeypadClearButtonPressedMessage args)
    {
        _audio.PlayPvs(component.KeypadPressSound, uid);
        component.CurrentCode = string.Empty;
        UpdateUi(uid, component);
    }

    private void OnCancelPressed(EntityUid uid, KeypadComponent component, KeypadCancelButtonPressedMessage args)
    {
        if (component.State == KeypadState.Normal)
            return;

        component.State = KeypadState.Normal;
        component.CurrentCode = string.Empty;
        UpdateUi(uid, component);
    }

    private void OnEnterPressed(EntityUid uid, KeypadComponent component, KeypadEnterButtonPressedMessage args)
    {
        switch (component.State)
        {
            case KeypadState.Normal:
                if (component.CurrentCode == component.CorrectCode)
                {
                    _audio.PlayPvs(component.AccessGrantedSound, uid);
                    _deviceLink.InvokePort(uid, component.UnlockedPort);
                }
                else
                {
                    _audio.PlayPvs(component.AccessDeniedSound, uid);
                }
                break;
            case KeypadState.AwaitingOldCode:
                if (component.CurrentCode == component.CorrectCode)
                {
                    component.State = KeypadState.AwaitingNewCode;
                    _audio.PlayPvs(component.AccessGrantedSound, uid);
                }
                else
                {
                    component.State = KeypadState.Normal;
                    _audio.PlayPvs(component.AccessDeniedSound, uid);
                }
                break;
            case KeypadState.AwaitingNewCode:
                component.CorrectCode = component.CurrentCode;
                component.State = KeypadState.Normal;
                _audio.PlayPvs(component.AccessGrantedSound, uid);
                break;
        }

        component.CurrentCode = string.Empty;
        UpdateUi(uid, component);
    }

    private void StartChangeCode(EntityUid uid, KeypadComponent component, EntityUid user)
    {
        component.State = KeypadState.AwaitingOldCode;
        component.CurrentCode = string.Empty;
        UpdateUi(uid, component);
    }

    private void UpdateUi(EntityUid uid, KeypadComponent component)
    {
        var state = new KeypadUISate(component.CurrentCode, component.State);
        _ui.SetUiState(uid, KeypadUiKey.Key, state);
    }
}
