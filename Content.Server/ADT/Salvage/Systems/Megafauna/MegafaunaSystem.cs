using Content.Server.Shuttles.Components;
using Content.Shared.ADT.Salvage.Components;
using Content.Shared.Weapons.Melee.Components;
using Robust.Shared.Map;

namespace Content.Server.ADT.Salvage.Systems;

public sealed partial class MegafaunaSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MegafaunaComponent, AttemptMeleeThrowOnHitEvent>(OnAttemptMeleeThrowOnHit);

        SubscribeLocalEvent<MegafaunaComponent, MapInitEvent>(OnMegafaunaMapInit);
        SubscribeLocalEvent<ADTLavalandBoundComponent, EntParentChangedMessage>(OnBoundParentChanged);

        InitializeDrake();
    }

    private void OnAttemptMeleeThrowOnHit(Entity<MegafaunaComponent> _, ref AttemptMeleeThrowOnHitEvent args)
    {
        args.Cancelled = true;
    }

    private void OnMegafaunaMapInit(Entity<MegafaunaComponent> ent, ref MapInitEvent args)
    {
        var bound = EnsureComp<ADTLavalandBoundComponent>(ent);
        if (Transform(ent).MapUid is { } map)
            bound.HomeMap = map;
    }

    private void OnBoundParentChanged(Entity<ADTLavalandBoundComponent> ent, ref EntParentChangedMessage args)
    {
        var xform = Transform(ent);

        if (!Exists(ent.Comp.HomeMap) && xform.MapUid is { } currentMap)
            ent.Comp.HomeMap = currentMap;

        if (!Exists(ent.Comp.HomeMap))
            return;

        if (xform.GridUid is not { } grid || !HasComp<ShuttleComponent>(grid))
            return;

        var worldPos = _transform.GetMapCoordinates(ent.Owner, xform).Position;
        var homeMapId = Transform(ent.Comp.HomeMap).MapID;
        _transform.SetMapCoordinates(ent.Owner, new MapCoordinates(worldPos, homeMapId));
    }
}
