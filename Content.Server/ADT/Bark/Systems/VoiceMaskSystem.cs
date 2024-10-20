// using Content.Shared.ADT.CCVar;
// using Content.Shared.VoiceMask;
// using Content.Server.ADT.SpeechBarks;
// using Content.Shared.ADT.SpeechBarks;
// using Robust.Shared.Configuration;

// namespace Content.Server.VoiceMask;

// public partial class VoiceMaskSystem
// {
//     [Dependency] private readonly IConfigurationManager _cfg = default!;
//     private void InitializeBarks()
//     {
//         SubscribeLocalEvent<VoiceMaskComponent, TransformSpeakerBarkEvent>(OnSpeakerVoiceTransform);
//         SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeBarkMessage>(OnChangeBark);
//         SubscribeLocalEvent<VoiceMaskComponent, VoiceMaskChangeBarkPitchMessage>(OnChangePitch);
//     }

//     private void OnSpeakerVoiceTransform(EntityUid uid, VoiceMaskComponent component, TransformSpeakerBarkEvent args)
//     {
//         if (component.Enabled)
//         {
//             args.Pitch = Math.Clamp(component.BarkPitch, _cfg.GetCVar(ADTCCVars.BarksMaxPitch), _cfg.GetCVar(ADTCCVars.BarksMaxPitch));

//             if (!_proto.TryIndex<BarkPrototype>(component.BarkId, out var proto))
//                 return;

//             args.Sound = proto.Sound;
//         }
//     }

//     private void OnChangeBark(EntityUid uid, VoiceMaskComponent component, VoiceMaskChangeBarkMessage message)
//     {
//         component.BarkId = message.Proto;

//         _popupSystem.PopupEntity(Loc.GetString("voice-mask-voice-popup-success"), uid);

//         TrySetLastKnownBark(uid, message.Proto);

//         UpdateUI(uid, component);
//     }

//     private void TrySetLastKnownBark(EntityUid maskWearer, string voiceId)
//     {
//         if (!HasComp<VoiceMaskComponent>(maskWearer)
//             || !_inventory.TryGetSlotEntity(maskWearer, MaskSlot, out var maskEntity)
//             || !TryComp<VoiceMaskerComponent>(maskEntity, out var maskComp))
//         {
//             return;
//         }

//         maskComp.LastSetVoice = voiceId;
//     }

//     private void OnChangePitch(EntityUid uid, VoiceMaskComponent component, VoiceMaskChangeBarkPitchMessage message)
//     {
//         if (!float.TryParse(message.Pitch, out var item))
//             return;

//         component.BarkPitch = item;

//         _popupSystem.PopupEntity(Loc.GetString("voice-mask-voice-popup-success"), uid);

//         TrySetLastKnownPitch(uid, item);

//         UpdateUI(uid, component);
//     }

//     private void TrySetLastKnownPitch(EntityUid maskWearer, float pitch)
//     {
//         if (!HasComp<VoiceMaskComponent>(maskWearer)
//             || !_inventory.TryGetSlotEntity(maskWearer, MaskSlot, out var maskEntity)
//             || !TryComp<VoiceMaskerComponent>(maskEntity, out var maskComp))
//         {
//             return;
//         }

//         maskComp.LastSetPitch = pitch;
//     }

// }
