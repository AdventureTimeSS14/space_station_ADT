using Content.Shared.Coordinates.Helpers;
using Robust.Shared.Map;

namespace Content.Shared.ADT.Areas;

public sealed class AreaSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly IMapManager _map = default!;

    private const float Range = 0.25f;
    private const LookupFlags Flags = LookupFlags.Static;

    private HashSet<Entity<AreaComponent>> _areas = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AreaComponent, AnchorStateChangedEvent>(OnAnchorStateChanged);
    }

    private void OnAnchorStateChanged(Entity<AreaComponent> ent, ref AnchorStateChangedEvent args)
    {
        // delete areas that get unanchored by explosions, someone removing the floor etc
        // don't do it if client is detaching or it will break PVS
        if (!args.Anchored && !args.Detaching)
            PredictedQueueDel(ent);
    }

    #region Public API

    /// <summary>
    /// Get the area a given mob is in.
    /// </summary>
    public EntityUid? GetArea(EntityUid target)
        => GetArea(Transform(target).Coordinates);

    /// <summary>
    /// Get the area at a given position.
    /// It will be snapped to the nearest tile, if your position is already snapped use <see cref="GetAreaCentered"/>.
    /// </summary>
    public EntityUid? GetArea(EntityCoordinates coords)
        => GetAreaCentered(coords.SnapToGrid(EntityManager, _map));

    /// <summary>
    /// Get the area at a given position which must be centered on a tile.
    /// Only call this if the coordinates are already centered on a tile.
    /// </summary>
    public EntityUid? GetAreaCentered(EntityCoordinates coords)
    {
        // TODO: if this is found to be expensive investigate:
        // A. storing which area(s) an entity is in through collisions (while map is unpaused)
        // B. having a quadtree etc to store areas instead of lookup
        // C. only using entities to map areas, store them on a special grid component similar to decals or tile air mixes
        _areas.Clear();
        _lookup.GetEntitiesInRange(coords, Range, _areas, Flags);
        foreach (var area in _areas)
        {
            return area; // return the first area, should only ever be 1 because of placement replacement
        }
        return null;
    }

    /// <summary>
    /// Returns the Prototype ID (string) of the area entity, or null if it has no prototype / is not an area.
    /// </summary>
    public string? GetAreaPrototypeId(EntityUid? areaUid)
    {
        if (areaUid == null || !HasComp<AreaComponent>(areaUid.Value))
            return null;

        return Prototype(areaUid.Value)?.ID;
    }

    /// <summary>
    /// Returns the Prototype ID (string) of the area at the given coordinates.
    /// </summary>
    public string? GetAreaPrototypeId(EntityCoordinates coords)
    {
        var area = GetArea(coords);
        return GetAreaPrototypeId(area);
    }

    /// <summary>
    /// Raises a by-ref event on the area a given mob is in.
    /// </summary>
    public void RaiseAreaEvent<T>(EntityUid target, ref T ev) where T: notnull
    {
        if (GetArea(target) is {} area)
            RaiseLocalEvent(area, ref ev);
    }

    /// <summary>
    /// Raises a by-ref event on the area at a given position.
    /// </summary>
    public void RaiseAreaEvent<T>(EntityCoordinates coords, ref T ev) where T: notnull
    {
        if (GetArea(coords) is {} area)
            RaiseLocalEvent(area, ref ev);
    }

    #endregion
}