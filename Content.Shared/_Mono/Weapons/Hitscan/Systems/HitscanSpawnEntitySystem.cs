using Content.Shared._Mono.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Hitscan.Systems;
using Robust.Shared.Network;

namespace Content.Shared._Mono.Weapons.Hitscan.Systems;

public sealed partial class HitscanSpawnEntitySystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HitscanSpawnEntityComponent, HitscanRaycastFiredEvent>(OnHitscanHit, after: [ typeof(HitscanReflectSystem) ]);
    }

    private void OnHitscanHit(Entity<HitscanSpawnEntityComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        // ADT-Tweak: one hit entity per event here, reflections never raise it
        if (_net.IsClient || args.Data.HitEntity is not { } hitEntity)
            return;

        Spawn(ent.Comp.SpawnedEntity, Transform(hitEntity).Coordinates);
    }
}
