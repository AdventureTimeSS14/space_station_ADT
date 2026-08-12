using Content.Server.ADT.Procedural;
using Content.Server.Procedural;
using Robust.Shared.Random;

namespace Content.Server.ADT.Generation;

public sealed class ADTRuinBeaconSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTRuinBeaconComponent, MapInitEvent>(OnMapInit, after: [typeof(RoomFillSystem), typeof(ADTRoomFillSystem)]);
    }

    private void OnMapInit(Entity<ADTRuinBeaconComponent> ent, ref MapInitEvent args)
    {
        if (!_random.Prob(ent.Comp.Probability))
            return;

        Spawn(ent.Comp.Beacon, Transform(ent).Coordinates);
    }
}
