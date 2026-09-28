using System.Linq;
using Content.Shared.ADT.InconnuOS;
using Content.Shared.ADT.InconnuOS.Components;
using Content.Shared.ADT.InconnuOS.NanoNet;
using Content.Shared.GameTicking;

namespace Content.Server.ADT.InconnuOS;

public sealed partial class ADTOsSystem
{
    private readonly Dictionary<string, NanoNetSite> _sites = new(StringComparer.OrdinalIgnoreCase);

    private sealed record NanoNetSite(string Owner, string Html, TimeSpan PublishedAt);

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

        var owner = GetUserName(args.Actor);

        if (_sites.TryGetValue(label, out var existing) && existing.Owner != owner)
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

            var ownedByThisPlayer = _sites.Values.Count(s => s.Owner == owner);
            if (ownedByThisPlayer >= NanoNetLimits.MaxSitesPerOwner)
            {
                Deny(ent, args.Actor, OsValidationError.TooManySites, NanoNetLimits.MaxSitesPerOwner.ToString());
                return;
            }
        }

        var publishedAt = _timing.CurTime;
        _sites[label] = new NanoNetSite(owner, args.Html, publishedAt);

        _ui.ServerSendUiMessage(ent.Owner, ADTComputerUiKey.Key,
            new ADTOsNanoNetStatusMessage(label, true, owner, publishedAt), args.Actor);
    }

    private void OnNanoNetUnpublish(Entity<ADTOperatingSystemComponent> ent, ref ADTOsNanoNetUnpublishMessage args)
    {
        if (!CanOperate(ent))
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

        if (existing.Owner != GetUserName(args.Actor))
        {
            Deny(ent, args.Actor, OsValidationError.DomainNotOwned, label);
            return;
        }

        _sites.Remove(label);

        _ui.ServerSendUiMessage(ent.Owner, ADTComputerUiKey.Key,
            new ADTOsNanoNetStatusMessage(label, false, string.Empty, TimeSpan.Zero), args.Actor);
    }

    private void OnNanoNetFetchRequest(Entity<ADTOperatingSystemComponent> ent, ref ADTOsNanoNetFetchRequestMessage args)
    {
        if (!CanOperate(ent) || !args.Actor.IsValid())
            return;

        if (_sites.TryGetValue(args.Domain, out var site))
        {
            _ui.ServerSendUiMessage(ent.Owner, ADTComputerUiKey.Key,
                new ADTOsNanoNetFetchResponseMessage(args.RequestId, args.Domain, args.Path, true, site.Html, site.Owner),
                args.Actor);
            return;
        }

        _ui.ServerSendUiMessage(ent.Owner, ADTComputerUiKey.Key,
            new ADTOsNanoNetFetchResponseMessage(args.RequestId, args.Domain, args.Path, false, string.Empty, string.Empty),
            args.Actor);
    }
}
