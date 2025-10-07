using Content.Shared.ADT.CCVar;
using Content.Shared.VoiceMask;
using Content.Shared.ADT.SpeechBarks;
using Robust.Shared.Configuration;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;

namespace Content.Server.VoiceMask;

public partial class VoiceMaskSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!; // Переименовано

    private void InitializeBarks()
    {
        SubscribeLocalEvent<VoiceMaskComponent, InventoryRelayedEvent<TransformSpeakerBarkEvent>>(OnSpeakerVoiceTransform);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeBarkMessage>(OnChangeBark);
        SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeBarkPitchMessage>(OnChangePitch);
    }

    private void OnSpeakerVoiceTransform(EntityUid uid, VoiceMaskComponent component, ref InventoryRelayedEvent<TransformSpeakerBarkEvent> args)
    {
        if (!_protoManager.TryIndex<BarkPrototype>(component.BarkId, out var proto)) // Исправлено
            return;

        args.Args.Data.Pitch = Math.Clamp(component.BarkPitch, _cfg.GetCVar(ADTCCVars.BarksMinPitch), _cfg.GetCVar(ADTCCVars.BarksMaxPitch));
        args.Args.Data.Sound = proto.Sound;
    }

    private void OnChangeBark(EntityUid uid, VoiceMaskComponent component, VoiceMaskChangeBarkMessage message)
    {
        if (!_protoManager.HasIndex<BarkPrototype>(message.Proto)) // Добавлена проверка
        {
            _popupSystem.PopupEntity(Loc.GetString("voice-mask-voice-popup-invalid"), uid);
            return;
        }

        component.BarkId = message.Proto;
        _popupSystem.PopupEntity(Loc.GetString("voice-mask-voice-popup-success"), uid);
        UpdateUI((uid, component));
    }

    private void OnChangePitch(EntityUid uid, VoiceMaskComponent component, VoiceMaskChangeBarkPitchMessage message)
    {
        if (!float.TryParse(message.Pitch, out var pitchValue))
        {
            _popupSystem.PopupEntity(Loc.GetString("voice-mask-voice-popup-invalid-pitch"), uid);
            return;
        }

        component.BarkPitch = pitchValue;
        _popupSystem.PopupEntity(Loc.GetString("voice-mask-voice-popup-success"), uid);
        UpdateUI((uid, component));
    }
}