//

using Content.Shared.Heretic;
using Content.Shared.Heretic.Components;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.ADT.Heretic.Systems;

public sealed partial class RaggedSwordSystem : EntitySystem
{
    [Dependency] private readonly SharedHereticSystem _heretic = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RaggedSwordComponent, MeleeHitEvent>(OnHit);
    }

    private void OnHit(Entity<RaggedSwordComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        if (!_heretic.TryGetHereticComponent(args.User, out var heretic, out _))
            return;

        foreach (var hit in args.HitEntities)
        {
            if (hit == args.User)
                continue;

            var mark = EnsureComp<HereticCombatMarkComponent>(hit);
            mark.DisappearTime = mark.MaxDisappearTime;
            mark.Path = heretic.CurrentPath ?? "Blade";
            mark.Repetitions = 1;
            Dirty(hit, mark);
        }
    }
}
