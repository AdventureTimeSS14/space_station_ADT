using Content.Shared.ADT.Heretic.Components;
using Content.Shared.CombatMode;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Heretic.Systems;

/// <summary>
///     ADT: from Goob Multihit. On hit, sweeps held items and follows up
///     with any that pass the whitelist (e.g. second Blade path blade).
/// </summary>
public sealed class MultihitSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MultihitComponent, MeleeHitEvent>(OnHit);

        SubscribeLocalEvent<MultihitUserHereticEvent>(HereticCheck);
        SubscribeLocalEvent<MultihitUserWhitelistEvent>(WhitelistCheck);
    }

    private void WhitelistCheck(MultihitUserWhitelistEvent ev)
    {
        // ADT: no IsBlacklistFail, invert manually
        ev.Handled = ev.Blacklist
            ? _whitelist.IsWhitelistFail(ev.Whitelist, ev.User)
            : _whitelist.IsWhitelistPass(ev.Whitelist, ev.User);
    }

    private void HereticCheck(MultihitUserHereticEvent args)
    {
        if (!_heretic.TryGetHereticComponent(args.User, out var heretic, out _))
            return;

        args.Handled = (args.RequiredPath == null || heretic.CurrentPath == args.RequiredPath)
                       && heretic.PathStage >= args.MinPathStage;
    }

    private void OnHit(Entity<MultihitComponent> ent, ref MeleeHitEvent args)
    {
        // owner client + server only, avoid prediction dupes
        if (_net.IsClient && _player.LocalEntity != args.User)
            return;

        if (!_timing.IsFirstTimePredicted || !args.IsHit || args.Weapon == args.User)
            return;

        // ADT: no heavy/wide attacks, no public ArcRayCast/AttemptHeavyAttack
        if (args.Direction != null)
            return;

        if (args.HitEntities.Count == 0 || args.HitEntities[0] == args.User)
            return;

        // already a follow-up hit, don't recurse
        if (HasComp<ActiveMultihitComponent>(ent.Owner))
            return;

        if (!CheckConditions(ent, args.User))
            return;

        var target = args.HitEntities[0];
        var user = args.User;
        var delay = ent.Comp.MultihitDelay;

        foreach (var held in _hands.EnumerateHeld(user))
        {
            if (held == ent.Owner)
                continue;

            if (ent.Comp.MultihitWhitelist != null && !_whitelist.IsValid(ent.Comp.MultihitWhitelist, held))
                continue;

            if (!TryComp(held, out MeleeWeaponComponent? melee))
                continue;

            EnsureComp<ActiveMultihitComponent>(held).DamageMultiplier *= ent.Comp.DamageMultiplier;

            var weapon = held;
            Timer.Spawn(delay, () => DoExtraAttack(weapon, user, target));

            delay += ent.Comp.MultihitDelay;
        }
    }

    private void DoExtraAttack(EntityUid weapon, EntityUid user, EntityUid target)
    {
        if (TerminatingOrDeleted(weapon) || !TryComp(weapon, out ActiveMultihitComponent? active))
            return;

        if (TerminatingOrDeleted(user)
            || TerminatingOrDeleted(target)
            || !TryComp(weapon, out MeleeWeaponComponent? melee)
            || !_hands.IsHolding(user, weapon))
        {
            RemComp(weapon, active);
            return;
        }

        // need combat mode on for the hit to land, restore after
        var inCombat = _combatMode.IsInCombatMode(user);
        if (!inCombat)
            _combatMode.SetInCombatMode(user, true);

        _melee.AttemptLightAttack(user, weapon, melee, target);

        if (!inCombat)
            _combatMode.SetInCombatMode(user, false);

        if (TryComp(weapon, out ActiveMultihitComponent? stillActive))
            RemComp(weapon, stillActive);
    }

    private bool CheckConditions(Entity<MultihitComponent> ent, EntityUid user)
    {
        if (ent.Comp.Conditions.Count == 0)
            return true;

        foreach (var ev in ent.Comp.Conditions)
        {
            ev.Handled = false;
            ev.User = user;
            RaiseLocalEvent(user, (object) ev, true);

            switch (ev.Handled)
            {
                case false when ent.Comp.RequireAllConditions:
                    return false;
                case true when !ent.Comp.RequireAllConditions:
                    return true;
            }
        }

        return ent.Comp.RequireAllConditions;
    }
}
