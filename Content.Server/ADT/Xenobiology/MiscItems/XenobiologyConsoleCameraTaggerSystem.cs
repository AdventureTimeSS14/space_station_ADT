using Content.Server.Chat.Managers;
using Content.Shared.ADT.Xenobiology.MiscItems;
using Content.Shared.Chat;
using Content.Shared.Interaction;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Robust.Shared.Player;

namespace Content.Server.ADT.Xenobiology.MiscItems;

/// <summary>
/// Tags cameras with the xenobiology vision network so the console eye can spy through them.
/// </summary>
public sealed partial class XenobiologyConsoleCameraTaggerSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly SharedStationAiSystem _stationAi = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<XenobiologyConsoleCameraTaggerComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<XenobiologyConsoleCameraTaggerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target)
            return;

        var vision = EnsureComp<StationAiVisionComponent>(target);
        _stationAi.SetVisionNetwork((target, vision), ent.Comp.VisionNetwork);

        if (TryComp<ActorComponent>(args.User, out var actor))
        {
            var message = Loc.GetString("xenobiology-camera-tagger-tagged", ("name", Name(target)));
            _chat.ChatMessageToOne(ChatChannel.Local, message, message, EntityUid.Invalid, false, actor.PlayerSession.Channel);
        }

        args.Handled = true;
    }
}
