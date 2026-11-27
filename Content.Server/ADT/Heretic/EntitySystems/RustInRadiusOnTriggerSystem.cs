using Content.Server.Heretic.Abilities;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Trigger;
using Robust.Shared.Map;

namespace Content.Server.ADT.Heretic.EntitySystems;

public sealed class RustInRadiusOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly HereticAbilitySystem _ability = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RustInRadiusOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<RustInRadiusOnTriggerComponent> ent, ref TriggerEvent args)
    {
        var mapPos = _transform.GetMapCoordinates(ent);
        _ability.RustObjectsInRadius(mapPos, ent.Comp.Range, ent.Comp.TileRune, ent.Comp.LookupRange, ent.Comp.RustStrength);
    }
}
