using Content.Shared.ADT.Construction.Components;
using Content.Shared.ADT.Lavaland;
using Content.Shared.Construction.Components;
using Content.Shared.Popups;

namespace Content.Shared.ADT.Construction.Systems;

public sealed class ADTBlockAnchorOnLavalandSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTBlockAnchorOnLavalandComponent, AnchorAttemptEvent>(OnAnchorAttempt);
        SubscribeLocalEvent<ADTBlockAnchorOnLavalandComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
    }

    private void OnAnchorAttempt(Entity<ADTBlockAnchorOnLavalandComponent> ent, ref AnchorAttemptEvent args)
    {
        if (args.Cancelled || !IsOnLavaland(ent))
            return;

        _popup.PopupPredicted(Loc.GetString("adt-anchor-blocked-on-lavaland"), ent, args.User);
        args.Cancel();
    }

    private void OnAnchorStateChanged(Entity<ADTBlockAnchorOnLavalandComponent> ent, ref AnchorStateChangedEvent args)
    {
        if (!args.Anchored || !IsOnLavaland(ent))
            return;

        _popup.PopupPredicted(Loc.GetString("adt-anchor-blocked-on-lavaland"), ent, null);
        _transform.Unanchor(ent, Transform(ent));
    }

    private bool IsOnLavaland(EntityUid uid)
    {
        return Transform(uid).MapUid is { } map && HasComp<ADTLavalandMapComponent>(map);
    }
}
