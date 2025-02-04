
using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.Ghost;
using Content.Shared.Radio;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Content.Shared.ADT.BattleShipsAnnouce;
using Robust.Shared.Player;
using System.Linq;

namespace Content.Server.ADT.BattleShipsAnnouce;

public sealed class BattleShipsAnnouceSystem : SharedAnnounceSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    public readonly SoundSpecifier AnnouncementSound = new SoundPathSpecifier("/Audio/Announcements/announce.ogg");
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void AnnounceKMT(string message, SoundSpecifier? sound = null)
    {
        var filterKMT = Filter.Empty()
            .AddWhereAttachedEntity(ent =>
                HasComp<KMTComponent>(ent) ||
                HasComp<GhostComponent>(ent)
            );

        _chatManager.ChatMessageToManyFiltered(filterKMT, ChatChannel.Radio, message, message, default, false, true, null);
        _audio.PlayGlobal(sound ?? AnnouncementSound, filterKMT, true, AudioParams.Default.WithVolume(-2f));
    }

    public override void AnnounceTSF(string message, SoundSpecifier? sound = null)
    {
        var filterKMT = Filter.Empty()
            .AddWhereAttachedEntity(ent =>
                HasComp<TSFComponent>(ent) ||
                HasComp<GhostComponent>(ent)
            );

        _chatManager.ChatMessageToManyFiltered(filterKMT, ChatChannel.Radio, message, message, default, false, true, null);
        _audio.PlayGlobal(sound ?? AnnouncementSound, filterKMT, true, AudioParams.Default.WithVolume(-2f));
    }


    public override void AnnounceBoth(string message, SoundSpecifier? sound = null)
    {
        var filter = Filter.Empty()
            .AddWhereAttachedEntity(ent =>
                HasComp<KMTComponent>(ent) ||
                HasComp<TSFComponent>(ent) ||
                HasComp<GhostComponent>(ent)
            );

        _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, message, default, false, true, null);
        _audio.PlayGlobal(sound ?? AnnouncementSound, filter, true, AudioParams.Default.WithVolume(-2f));
    }

    public void CountAnnouce()
    {
        var _TSFCount = EntityQuery<TSFComponent>().Count();
        var _KMTCount = EntityQuery<KMTComponent>().Count();

        if (_TSFCount <= 0 || _KMTCount <= 0)
            return;

        var message = $"TSF: {_TSFCount}, KMT: {_KMTCount}";

        AnnounceBoth(message);
    }
}
//на будущее
