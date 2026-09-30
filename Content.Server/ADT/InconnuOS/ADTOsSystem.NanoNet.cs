using Content.Server.ADT.InconnuOS.NanoNet;
using Content.Shared.ADT.InconnuOS;
using Content.Shared.ADT.InconnuOS.Components;
using Content.Shared.ADT.InconnuOS.NanoNet;
using Robust.Server.Player;

namespace Content.Server.ADT.InconnuOS;

public sealed partial class ADTOsSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly NanoNetSystem _nanoNet = default!;

    partial void InitializeNanoNet()
    {
        SubscribeLocalEvent<ADTOperatingSystemComponent, ADTOsNanoNetPublishMessage>(OnNanoNetPublish);
        SubscribeLocalEvent<ADTOperatingSystemComponent, ADTOsNanoNetUnpublishMessage>(OnNanoNetUnpublish);
        SubscribeLocalEvent<ADTOperatingSystemComponent, ADTOsNanoNetFetchRequestMessage>(OnNanoNetFetchRequest);
    }

    private void OnNanoNetPublish(Entity<ADTOperatingSystemComponent> ent, ref ADTOsNanoNetPublishMessage args)
    {
        if (!CanOperate(ent))
            return;

        if (!_playerManager.TryGetSessionByEntity(args.Actor, out var session))
            return;

        var ownerName = GetUserName(args.Actor);

        if (!_nanoNet.TryPublish(args.Actor, session, ownerName, args.Domain, args.Html,
                out var label, out var publishedAt, out var error, out var detail))
        {
            Deny(ent, args.Actor, error, detail);
            return;
        }

        _ui.ServerSendUiMessage(ent.Owner, ADTComputerUiKey.Key,
            new ADTOsNanoNetStatusMessage(args.RequestId, label, true, ownerName, publishedAt), args.Actor);
    }

    private void OnNanoNetUnpublish(Entity<ADTOperatingSystemComponent> ent, ref ADTOsNanoNetUnpublishMessage args)
    {
        if (!CanOperate(ent))
            return;

        if (!_playerManager.TryGetSessionByEntity(args.Actor, out var session))
            return;

        if (!_nanoNet.TryUnpublish(args.Actor, session.UserId, args.Domain, out var label, out var error, out var detail))
        {
            Deny(ent, args.Actor, error, detail);
            return;
        }

        _ui.ServerSendUiMessage(ent.Owner, ADTComputerUiKey.Key,
            new ADTOsNanoNetStatusMessage(args.RequestId, label, false, string.Empty, TimeSpan.Zero), args.Actor);
    }

    private void OnNanoNetFetchRequest(Entity<ADTOperatingSystemComponent> ent, ref ADTOsNanoNetFetchRequestMessage args)
    {
        if (!CanOperate(ent))
            return;

        var page = _nanoNet.Fetch(args.Url);

        _ui.ServerSendUiMessage(ent.Owner, ADTComputerUiKey.Key,
            new ADTOsNanoNetFetchResponseMessage(args.RequestId, page.Url, page.DisplayUrl, page.Found, page.Html),
            args.Actor);
    }
}
