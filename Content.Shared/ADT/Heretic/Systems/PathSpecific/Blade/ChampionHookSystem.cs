//

using Content.Shared.CombatMode;
using Content.Shared.Hands.Components;
using Content.Shared.Heretic.Components;
using Content.Shared.Heretic.Components.PathSpecific.Blade;
using Content.Shared.ADT.Heretic.Systems;
using Content.Shared.ADT.Heretic.Systems.PathSpecific.Lock;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.ADT.Heretic.Systems.PathSpecific.Blade;

public sealed partial class ChampionHookSystem : EntitySystem
{
    [Dependency] private readonly SharedCombatModeSystem _combat = default!;
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly BurglarsFinesseSystem _burglars = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandsComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerb);
        SubscribeLocalEvent<ChampionHookComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<ChampionHookComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent) || ent.Comp.HookedMob is not { } hooked)
            return;

        if (TryComp(hooked, out PullableComponent? pullable))
            _pulling.TryStopPull(hooked, pullable);
    }

    private void OnGetAltVerb(Entity<HandsComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        _burglars.AddStealVerb(ent, ref args);

        var user = args.User;
        if (user == ent.Owner || args.Using is not { } used || !args.CanAccess || !args.CanInteract)
            return;

        if (!HasComp<ChampionHookComponent>(user) || !HasComp<HereticBladeComponent>(used) || !_combat.IsInCombatMode(user))
            return;

        if (!_heretic.TryGetHereticComponent(user, out var heretic, out _) || heretic.CurrentPath != "Blade")
            return;

        args.Verbs.Add(new AlternativeVerb
        {
            Priority = 9,
            Act = () => DoHook(user, used, ent),
        });
    }

    private void DoHook(EntityUid user, EntityUid used, Entity<HandsComponent> target)
    {
        if (!TryComp(user, out ChampionHookComponent? hook) || hook.HookedMob != null)
            return;

        if (!TryComp(used, out MeleeWeaponComponent? melee))
            return;

        if (!_melee.AttemptLightAttack(user, used, melee, target))
            return;

        melee.NextAttack += TimeSpan.FromSeconds(1f / _melee.GetAttackRate(used, user, melee));
        Dirty(used, melee);

        _audio.PlayPredicted(hook.Sound, target, user);
        _pulling.StopAllPulls(user, stopPullable: false);

        if (!_stun.TryKnockdown(target.Owner, hook.KnockdownTime, autoStand: false))
            return;

        if (!_pulling.TryStartPull(user, target, force: true))
            return;

        hook.HookedMob = target;
        hook.Weapon = used;
        Dirty(user, hook);
    }
}
