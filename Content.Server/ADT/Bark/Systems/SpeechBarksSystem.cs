using Robust.Shared.Prototypes;
using Content.Shared.ADT.SpeechBarks;
using Content.Server.Chat.Systems;
using Robust.Shared.Configuration;
using Content.Shared.ADT.CCVar;
using Content.Server.Mind;
using Robust.Shared.Player;

namespace Content.Server.ADT.SpeechBarks;

public sealed class SpeechBarksSystem : SharedSpeechBarksSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
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
        if (comp.Data.Sound != string.Empty)
            return;
        comp.Data.Sound = _proto.Index(comp.Data.Proto).Sound;
    }

    private void OnEntitySpoke(EntityUid uid, SpeechBarksComponent component, EntitySpokeEvent args)
    {
        if (!_isEnabled)
            return;

        var ev = new TransformSpeakerBarkEvent(uid, component.Data.Copy());
        RaiseLocalEvent(uid, ev);

        var message = args.ObfuscatedMessage ?? args.Message;

        foreach (var ent in _lookup.GetEntitiesInRange(Transform(uid).Coordinates, 10f))
        {
            if (!_mind.TryGetMind(ent, out _, out var mind) || mind.UserId == null || !_player.TryGetSessionById(mind.UserId, out var session))
                continue;

            RaiseNetworkEvent(new PlaySpeechBarksEvent(
                        GetNetEntity(uid),
                        message,
                        ev.Data.Sound,
                        ev.Data.Pitch,
                        ev.Data.MinVar,
                        ev.Data.MaxVar,
                        args.Whisper), session);
        }
    }
}
