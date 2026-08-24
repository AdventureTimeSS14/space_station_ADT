// Ported from RMC-14 (https://github.com/RMC-14/RMC-14), MIT License
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Trigger;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Atmos;

public sealed class TileFireOnTriggerSystem : XOnTriggerSystem<TileFireOnTriggerComponent>
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly SharedRMCFlammableSystem _flammable = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override void OnTrigger(Entity<TileFireOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        var coords = _transform.GetMoverCoordinates(target);
        _audio.PlayPvs(ent.Comp.Sound, coords);

        var tile = coords.SnapToGrid(EntityManager, _map);
        _flammable.SpawnFireDiamond(ent.Comp.Spawn, tile, ent.Comp.Range, ent.Comp.Intensity, ent.Comp.Duration);

        args.Handled = true;
        QueueDel(ent.Owner);
    }
}
