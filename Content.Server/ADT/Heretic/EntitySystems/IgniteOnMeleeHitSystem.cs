//

using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Heretic.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.Heretic.EntitySystems;

public sealed class IgniteOnMeleeHitSystem : EntitySystem
{
    [Dependency] private readonly FlammableSystem _flammable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IgniteOnMeleeHitComponent, MeleeHitEvent>(OnHit);
    }

    private void OnHit(Entity<IgniteOnMeleeHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        foreach (var hit in args.HitEntities)
        {
            if (!TryComp(hit, out FlammableComponent? flammable))
                continue;

            _flammable.AdjustFireStacks(hit, ent.Comp.FireStacks, flammable, ignite: true);
        }
    }
}
