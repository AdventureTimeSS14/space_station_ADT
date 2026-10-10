using Content.Server.ADT.Procedural;
using Content.Server.Procedural;
using Robust.Shared.Random;

namespace Content.Server.ADT.Generation;

public sealed class ADTRuinBeaconSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTRuinBeaconComponent, MapInitEvent>(OnMapInit, after: [typeof(RoomFillSystem), typeof(ADTRoomFillSystem)]);
    }

    private void OnMapInit(Entity<ADTRuinBeaconComponent> ent, ref MapInitEvent args)
    {
        var xform = Transform(ent);

        if (xform.GridUid == null)
            return;

        if (!_random.Prob(ent.Comp.Probability))
            return;

        Spawn(ent.Comp.Beacon, xform.Coordinates);
    }
}
