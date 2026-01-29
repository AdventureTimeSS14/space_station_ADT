using Content.Shared.Humanoid;
using Content.Shared.ADT.StampHit;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Paper;

namespace Content.Server.ADT.StampHit;

public sealed class StampHitSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StampComponent, MeleeHitEvent>(OnMeleeHit);
    }

    public void OnMeleeHit(EntityUid uid, StampComponent comp, MeleeHitEvent args)
    {
        foreach (var i in args.HitEntities)
        {
            if (HasComp<HumanoidAppearanceComponent>(i))
            {
                if (!HasComp<StampedEntityComponent>(i))
                {
                    AddComp<StampedEntityComponent>(i);
                    if (TryComp<StampedEntityComponent>(i, out var entStamped))
                    {
                        entStamped.StampToEntity.Add(comp.StampedName);
                    }
                }
                else if (TryComp<StampedEntityComponent>(i, out var entStamped))
                {
                    entStamped.StampToEntity.Add(comp.StampedName);
                }
            }
        }
    }
}
