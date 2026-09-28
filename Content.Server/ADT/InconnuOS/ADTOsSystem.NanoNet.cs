using System.Linq;
using Content.Shared.ADT.InconnuOS;
using Content.Shared.ADT.InconnuOS.Components;
using Content.Shared.ADT.InconnuOS.NanoNet;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Network;

namespace Content.Server.ADT.InconnuOS;

public sealed partial class ADTOsSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private readonly Dictionary<string, NanoNetSite> _sites = new(StringComparer.OrdinalIgnoreCase);

    private sealed record NanoNetSite(NetUserId OwnerId, string OwnerName, string Html, TimeSpan PublishedAt);

    partial void InitializeNanoNet()
    {
        SubscribeLocalEvent<ADTOperatingSystemComponent, ADTOsNanoNetPublishMessage>(OnNanoNetPublish);
        SubscribeLocalEvent<ADTOperatingSystemComponent, ADTOsNanoNetUnpublishMessage>(OnNanoNetUnpublish);
        SubscribeLocalEvent<ADTOperatingSystemComponent, ADTOsNanoNetFetchRequestMessage>(OnNanoNetFetchRequest);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnNanoNetRoundRestart);
    }

    private void OnNanoNetRoundRestart(RoundRestartCleanupEvent args)
    {
        _sites.Clear();
    }

    private void OnNanoNetPublish(Entity<ADTOperatingSystemComponent> ent, ref ADTOsNanoNetPublishMessage args)
    {
        if (!CanOperate(ent))
            return;

        if (!_playerManager.TryGetSessionByEntity(args.Actor, out var session))
            return;

        if (!NanoNetDomain.TryNormalizeLabel(args.Domain, out var label, out var error))
        {
            Deny(ent, args.Actor, error, args.Domain);
            return;
        }

        if (args.Html.Length > NanoNetLimits.MaxSiteLength)
        {
            Deny(ent, args.Actor, OsValidationError.SiteTooLarge, NanoNetLimits.MaxSiteLength.ToString());
            return;
        }

        var ownerId = session.UserId;

        if (_sites.TryGetValue(label, out var existing) && existing.OwnerId != ownerId)
        {
            Deny(ent, args.Actor, OsValidationError.DomainNotOwned, label);
            return;
        }

        if (existing == null)
        {
            if (_sites.Count >= NanoNetLimits.MaxSitesTotal)
            {
                Deny(ent, args.Actor, OsValidationError.TooManySites, NanoNetLimits.MaxSitesTotal.ToString());
                return;
            }

            var ownedByThisPlayer = _sites.Values.Count(s => s.OwnerId == ownerId);
            if (ownedByThisPlayer >= NanoNetLimits.MaxSitesPerOwner)
            {
                Deny(ent, args.Actor, OsValidationError.TooManySites, NanoNetLimits.MaxSitesPerOwner.ToString());
                return;
            }
        }

        var ownerName = GetUserName(args.Actor);
        var publishedAt = _timing.CurTime;
        _sites[label] = new NanoNetSite(ownerId, ownerName, args.Html, publishedAt);

        _ui.ServerSendUiMessage(ent.Owner, ADTComputerUiKey.Key,
            new ADTOsNanoNetStatusMessage(args.RequestId, label, true, ownerName, publishedAt), args.Actor);
    }

    private void OnNanoNetUnpublish(Entity<ADTOperatingSystemComponent> ent, ref ADTOsNanoNetUnpublishMessage args)
    {
        if (!CanOperate(ent))
            return;

        if (!_playerManager.TryGetSessionByEntity(args.Actor, out var session))
            return;

        if (!NanoNetDomain.TryNormalizeLabel(args.Domain, out var label, out var error))
        {
            Deny(ent, args.Actor, error, args.Domain);
            return;
        }

        if (!_sites.TryGetValue(label, out var existing))
        {
            Deny(ent, args.Actor, OsValidationError.DomainNotFound, label);
            return;
        }

        if (existing.OwnerId != session.UserId)
        {
            Deny(ent, args.Actor, OsValidationError.DomainNotOwned, label);
            return;
        }

        _sites.Remove(label);

        _ui.ServerSendUiMessage(ent.Owner, ADTComputerUiKey.Key,
            new ADTOsNanoNetStatusMessage(args.RequestId, label, false, string.Empty, TimeSpan.Zero), args.Actor);
    }

    private void OnNanoNetFetchRequest(Entity<ADTOperatingSystemComponent> ent, ref ADTOsNanoNetFetchRequestMessage args)
    {
        if (!CanOperate(ent) || !args.Actor.IsValid())
            return;

        var lookup = args.Domain.Trim().ToLowerInvariant();

        if (_sites.TryGetValue(lookup, out var site))
        {
            _ui.ServerSendUiMessage(ent.Owner, ADTComputerUiKey.Key,
                new ADTOsNanoNetFetchResponseMessage(args.RequestId, args.Domain, args.Path, true, site.Html, site.OwnerName),
                args.Actor);
            return;
        }

        _ui.ServerSendUiMessage(ent.Owner, ADTComputerUiKey.Key,
            new ADTOsNanoNetFetchResponseMessage(args.RequestId, args.Domain, args.Path, false, string.Empty, string.Empty),
            args.Actor);
    }
}
