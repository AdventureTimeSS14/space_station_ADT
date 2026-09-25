using Content.Shared.ADT.Mining.Drill;

namespace Content.Server.ADT.Mining.Drill;

public sealed class ADTDrillBraceSystem : EntitySystem
{
    [Dependency] private readonly ADTDrillSystem _drillSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ADTDrillBraceComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ADTDrillBraceComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<ADTDrillComponent, ComponentInit>(OnDrillInit);
        SubscribeLocalEvent<ADTDrillComponent, AnchorStateChangedEvent>(OnDrillAnchorChanged);
    }

    private void OnComponentInit(EntityUid uid, ADTDrillBraceComponent component, ComponentInit args)
    {
        UpdateConnection(uid);
    }

    private void OnAnchorChanged(EntityUid uid, ADTDrillBraceComponent component, ref AnchorStateChangedEvent args)
    {
        UpdateConnection(uid);
    }

    private void OnDrillInit(EntityUid uid, ADTDrillComponent component, ComponentInit args)
    {
        RefreshAdjacentBraces(uid);
    }

    private void OnDrillAnchorChanged(EntityUid uid, ADTDrillComponent component, ref AnchorStateChangedEvent args)
    {
        RefreshAdjacentBraces(uid);
    }

    private void RefreshAdjacentBraces(EntityUid uid)
    {
        _drillSystem.ForEachAdjacentAnchored(Transform(uid), ent =>
        {
            if (HasComp<ADTDrillBraceComponent>(ent))
                UpdateConnection(ent);
        });
    }

    private void UpdateConnection(EntityUid uid)
    {
        var xform = Transform(uid);

        _drillSystem.RefreshAdjacentDrills(uid, xform, out var connectedUid);

        if (connectedUid.IsValid())
        {
            var delta = Transform(connectedUid).Coordinates.Position - xform.Coordinates.Position;
            _transform.SetLocalRotation(uid, delta.ToWorldAngle());
        }

        if (TryComp<AppearanceComponent>(uid, out _))
            _appearance.SetData(uid, ADTDrillBraceVisuals.Connected, connectedUid.IsValid());
    }
}