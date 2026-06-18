using Robust.Shared.Prototypes;
using Content.Shared.ADT.SpeechBarks;
using Content.Server.Chat.Systems;
using Robust.Shared.Configuration;
using Content.Shared.ADT.CCVar;
using Content.Server.Mind;
using Robust.Shared.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Chat;
using Content.Shared.Speech; // TSF edit
using Content.Shared.Inventory; // TSF edit

namespace Content.Server.ADT.SpeechBarks;

public sealed class SpeechBarksSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly InventorySystem _inventory = default!; // TSF edit
    [Dependency] private readonly SharedAudioSystem _audio = default!; // TSF edit
    private bool _isEnabled = false;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(ADTCCVars.BarksEnabled, v => _isEnabled = v, true);

        SubscribeLocalEvent<SpeechBarksComponent, EntitySpokeEvent>(OnEntitySpoke);
    }

    private void OnEntitySpoke(EntityUid uid, SpeechBarksComponent component, EntitySpokeEvent args)
    {
        if (!_isEnabled)
            return;

        var ev = new TransformSpeakerBarkEvent(uid, component.Data.Copy());
        RaiseLocalEvent(uid, ev);

        var message = args.ObfuscatedMessage ?? args.Message;
        // TSF edit start
        var isWhisper = args.ObfuscatedMessage != null;

        SoundSpecifier? soundSpecifier = null;

        if (_inventory.TryGetSlotEntity(uid, "mask", out var maskEntity))
        {
            if (TryComp<SpeechComponent>(maskEntity, out var speech) && speech.SpeechSounds?.Id == "Combine")
            {
                if (_proto.TryIndex<SpeechSoundsPrototype>(speech.SpeechSounds, out var speechSounds))
                {
                    var msgText = args.Message ?? "";

                    if (isWhisper)
                    {
                        soundSpecifier = speechSounds.SaySound;
                    }
                    else if (msgText.Contains('?'))
                    {
                        soundSpecifier = speechSounds.AskSound;
                    }
                    else if (msgText.Contains('!'))
                    {
                        soundSpecifier = speechSounds.ExclaimSound;
                    }
                    else
                    {
                        soundSpecifier = speechSounds.SaySound;
                    }
                }
            }
        }

        if (soundSpecifier != null)
        {
            _audio.PlayPvs(soundSpecifier, uid, AudioParams.Default.WithVolume(0.8f));
            return;
        }

        var defaultSound = ev.Data.Sound ?? _proto.Index(ev.Data.Proto).Sound;
        // TSF edit end
        foreach (var ent in _lookup.GetEntitiesInRange(Transform(uid).Coordinates, 10f))
        {
            if (!_mind.TryGetMind(ent, out _, out var mind) || mind.UserId == null || !_player.TryGetSessionById(mind.UserId, out var session))
                continue;

            RaiseNetworkEvent(new PlaySpeechBarksEvent(
                        GetNetEntity(uid),
                        message,
                        defaultSound,
                        ev.Data.Pitch,
                        ev.Data.MinVar,
                        ev.Data.MaxVar,
                        isWhisper), session);
        }
    }
}
