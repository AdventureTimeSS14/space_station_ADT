using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Paper;

namespace Content.Shared.ADT.StampHit;

public sealed class StampHitSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StampComponent, MeleeHitEvent>(OnMeleeHit);
    }

    public async void OnMeleeHit(EntityUid uid, StampComponent comp, MeleeHitEvent args)
    {
        foreach (var i in args.HitEntities)
        {
            AddComp<StampedEntityComponent>(i);
            if (TryComp<StampedEntityComponent>(i, out var EntStamped))
            {
                EntStamped.StampToEntity.Add(comp.StampedName);
            }
        }
    }
}
