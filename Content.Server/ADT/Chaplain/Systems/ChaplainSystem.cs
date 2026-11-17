using Content.Server.Chaplain;
using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.ADT.Chaplain.Systems;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Chaplain;

public sealed class ChaplainSystem : SharedChaplainSystem
{
    [Dependency] private EuiManager _euiManager = default!;
    [Dependency] private IChatManager _chatManager = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    private static readonly SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/Ambience/ambiholy.ogg");
    private static readonly Color BriefingColor = Color.Gold;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (!HasComp<ChaplainComponent>(args.Mob))
            return;

        var message = Loc.GetString("chaplain-briefing");
        var wrapped = Loc.GetString("chaplain-briefing-wrapped", ("message", FormattedMessage.EscapeText(message)));
        _chatManager.ChatMessageToOne(ChatChannel.Server, message, wrapped, default, false, args.Player.Channel, BriefingColor);
        _audio.PlayGlobal(BriefingSound, args.Player);
    }

    protected override void OfferReligion(Entity<ChaplainComponent> ent, EntityUid target)
    {
        if (!_mind.TryGetMind(target, out _, out var mind) || !_player.TryGetSessionById(mind.UserId, out var session))
            return;

        _euiManager.OpenEui(new AcceptReligionEui(ent, target, this), session);
    }
}
