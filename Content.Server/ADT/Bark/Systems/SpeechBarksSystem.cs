using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Containers;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Content.Shared.Hands.EntitySystems;
using System.Linq;
using Content.Shared.ADT.SpeechBarks;
using Content.Server.Chat.Systems;
using Robust.Shared.Configuration;
using Content.Shared.ADT.CCVar;
using Robust.Shared.Utility;
using System.Threading.Tasks;

namespace Content.Server.ADT.SpeechBarks;

public sealed class SpeechBarksSystem : SharedSpeechBarksSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private bool _isEnabled = false;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(ADTCCVars.BarksEnabled, v => _isEnabled = v, true);

        SubscribeLocalEvent<SpeechBarksComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SpeechBarksComponent, EntitySpokeEvent>(OnEntitySpoke);

    }
    private void OnMapInit(EntityUid uid, SpeechBarksComponent comp, MapInitEvent args)
    {
        if (comp.BarkPrototype != null && comp.BarkPrototype != String.Empty)
        {
            var proto = _proto.Index(comp.BarkPrototype.Value);
            comp.Sound = proto.Sound;
        }
    }

    private void OnEntitySpoke(EntityUid uid, SpeechBarksComponent component, EntitySpokeEvent args)
    {
        if (!_isEnabled)
            return;

        var ev = new TransformSpeakerBarkEvent(uid, component.Sound, component.BarkPitch);
        RaiseLocalEvent(uid, ev);

        var message = args.ObfuscatedMessage ?? args.Message;

        PlayBarks(new PlaySpeechBarksEvent(
            GetNetEntity(uid),
            message,
            ev.Sound,
            ev.Pitch,
            component.BarkLowVar,
            component.BarkHighVar,
            args.Whisper));
    }

    private async void PlayBarks(PlaySpeechBarksEvent ev)
    {
        if (ev.Message == null)
            return;

        var count = (int)ev.Message.Length / 3f;

        for (var i = 0; i < count; i++)
        {
            RaiseNetworkEvent(ev);

            await Task.Delay(TimeSpan.FromSeconds(_random.NextFloat(ev.LowVar, ev.HighVar)));
        }
    }
}
