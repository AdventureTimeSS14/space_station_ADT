using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Content.Shared.Weapons.Hitscan.Components;

namespace Content.Server.Weapons.Hitscan;

public sealed class HitscanIgniteSystem : EntitySystem
{
    [Dependency] private readonly FlammableSystem _flammable = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HitscanIgniteComponent, HitscanRaycastFiredEvent>(OnHitscanHit);
    }

    private void OnHitscanHit(Entity<HitscanIgniteComponent> ent, ref HitscanRaycastFiredEvent args)
    {
        if (args.Data.HitEntity is not { } target)
            return;

        if (!TryComp<FlammableComponent>(target, out var flammable))
            return;

        _flammable.AdjustFireStacks(target, ent.Comp.FireStacks, flammable, ignite: true);
    }
}
