using System.Linq;
using Content.Shared.ADT.Mining.Components;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs.Components;
using Robust.Shared.Containers;

namespace Content.Server.ADT.Mining;

public sealed class ADTMagmiteScannerSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private const int CarrierSearchDepth = 8;

    private readonly List<EntityUid> _chain = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTMagmiteScannerComponent, ComponentStartup>(OnScannerStartup);
        SubscribeLocalEvent<ADTMagmiteScannerComponent, ComponentShutdown>(OnScannerShutdown);
        SubscribeLocalEvent<ADTMagmiteScannerComponent, EntParentChangedMessage>(OnScannerParentChanged);
        SubscribeLocalEvent<ADTMagmiteScannerComponent, ItemToggledEvent>(OnScannerToggled);

        SubscribeLocalEvent<ADTMagmiteScannerLinkComponent, EntParentChangedMessage>(OnLinkParentChanged);
    }

    private void OnScannerStartup(Entity<ADTMagmiteScannerComponent> ent, ref ComponentStartup args)
    {
        Refresh(ent);
    }

    private void OnScannerShutdown(Entity<ADTMagmiteScannerComponent> ent, ref ComponentShutdown args)
    {
        _chain.Clear();
        SetChain(ent, _chain);

        if (ent.Comp.User is not { } user)
            return;

        ent.Comp.User = null;
        RefreshViewer(user);
    }

    private void OnScannerParentChanged(Entity<ADTMagmiteScannerComponent> ent, ref EntParentChangedMessage args)
    {
        Refresh(ent);
    }

    private void OnScannerToggled(Entity<ADTMagmiteScannerComponent> ent, ref ItemToggledEvent args)
    {
        Refresh(ent);
    }

    private void OnLinkParentChanged(Entity<ADTMagmiteScannerLinkComponent> ent, ref EntParentChangedMessage args)
    {
        foreach (var scanner in ent.Comp.Scanners.ToArray())
        {
            if (TryComp<ADTMagmiteScannerComponent>(scanner, out var comp))
                Refresh((scanner, comp));
        }
    }

    private void Refresh(Entity<ADTMagmiteScannerComponent> ent)
    {
        var user = IsActive(ent.Owner) ? FindCarrier(ent.Owner, _chain) : null;

        if (user == null)
            _chain.Clear();

        SetChain(ent, _chain);

        if (ent.Comp.User == user)
            return;

        var previous = ent.Comp.User;
        ent.Comp.User = user;

        if (previous is { } old)
            RefreshViewer(old);

        if (user is { } current)
            RefreshViewer(current);
    }

    private void RefreshViewer(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid))
            return;

        float? range = null;

        var query = EntityQueryEnumerator<ADTMagmiteScannerComponent>();

        while (query.MoveNext(out _, out var scanner))
        {
            if (scanner.User != uid)
                continue;

            if (range == null || scanner.Range > range.Value)
                range = scanner.Range;
        }

        if (range is not { } value)
        {
            RemComp<ADTMagmiteScannerViewerComponent>(uid);
            return;
        }

        var viewer = EnsureComp<ADTMagmiteScannerViewerComponent>(uid);
        viewer.Range = value;
        Dirty(uid, viewer);
    }

    private EntityUid? FindCarrier(EntityUid scanner, List<EntityUid> chain)
    {
        chain.Clear();

        var current = scanner;

        for (var depth = 0; depth < CarrierSearchDepth; depth++)
        {
            if (!_container.TryGetContainingContainer((current, null, null), out var container))
                return null;

            var parent = container.Owner;

            if (HasComp<MobStateComponent>(parent))
                return parent;

            chain.Add(parent);
            current = parent;
        }

        return null;
    }

    private void SetChain(Entity<ADTMagmiteScannerComponent> ent, List<EntityUid> chain)
    {
        foreach (var link in ent.Comp.Chain)
        {
            if (chain.Contains(link))
                continue;

            if (!TryComp<ADTMagmiteScannerLinkComponent>(link, out var comp))
                continue;

            comp.Scanners.Remove(ent.Owner);

            if (comp.Scanners.Count == 0)
                RemCompDeferred(link, comp);
        }

        foreach (var link in chain)
        {
            EnsureComp<ADTMagmiteScannerLinkComponent>(link).Scanners.Add(ent.Owner);
        }

        ent.Comp.Chain.Clear();
        ent.Comp.Chain.AddRange(chain);
    }

    private bool IsActive(EntityUid uid)
    {
        return !TryComp<ItemToggleComponent>(uid, out var toggle) || toggle.Activated;
    }
}
