//

using Content.Shared.CombatMode;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Heretic.Components;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Player;

namespace Content.Shared.ADT.Heretic.Systems.PathSpecific.Lock;

public sealed partial class BurglarsFinesseSystem : EntitySystem
{
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public void AddStealVerb(Entity<HandsComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;
        if (user == ent.Owner || args.Using is not { } used || !args.CanAccess || !args.CanInteract)
            return;

        if (!CanSteal(user, used))
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Priority = 9,
            Act = () => DoSteal(user, used, ent)
        });
    }

    private bool CanSteal(EntityUid user, EntityUid used)
    {
        return _heretic.TryGetHereticComponent(user, out var heretic, out _) &&
               heretic is { CurrentPath: "Lock", PathStage: >= 6 } && HasComp<HereticBladeComponent>(used) &&
               _combat.IsInCombatMode(user);
    }

    private void DoSteal(EntityUid user, EntityUid used, Entity<HandsComponent> target)
    {
        if (!TryComp(used, out MeleeWeaponComponent? melee))
            return;

        if (!_melee.AttemptLightAttack(user, used, melee, target))
            return;

        melee.NextAttack += TimeSpan.FromSeconds(1f / _melee.GetAttackRate(used, user, melee));
        Dirty(used, melee);

        var t = target.AsNullable();
        foreach (var held in _hands.EnumerateHeld(t))
        {
            if (!_hands.TryDrop(t, held, doDropInteraction: false))
                continue;

            _hands.TryPickupAnyHand(user, held, checkActionBlocker: false);
            break;
        }
    }
}
