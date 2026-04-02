using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.ADT.StampHit;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Paper;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.StampHit;

public sealed class StampHitSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StampComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(EntityUid uid, StampComponent comp, MeleeHitEvent args)
    {
        foreach (var i in args.HitEntities)
        {
            if (!TryComp<HumanoidAppearanceComponent>(i, out var speciesComp))
                continue;
            if (speciesComp.Species != default && speciesComp.Species == "SlimePerson" || speciesComp.Species == "NovakidSpecies")
                continue;
            if (HasComp<HumanoidAppearanceComponent>(i))
            {
                if (!HasComp<StampedEntityComponent>(i))
                {
                    EnsureComp<StampedEntityComponent>(i);
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
