using Content.Server.Bible.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Popups;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.Ghost.Roles.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Content.Shared.ADT.Controlled;
using Content.Shared.Mind;

namespace Content.Server.Bible;

public sealed class BibleSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly SharedControlledSystem _controlled = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ChaplainSystem _chaplain = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BibleComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SummonableComponent, GetVerbsEvent<AlternativeVerb>>(AddSummonVerb);
        SubscribeLocalEvent<FamiliarComponent, MobStateChangedEvent>(OnFamiliarDeath);
        SubscribeLocalEvent<FamiliarComponent, GhostRoleSpawnerUsedEvent>(OnSpawned);
    }
    // Too much changes and I forgor what is not changed.
    // ADT File.
    private void OnAfterInteract(EntityUid uid, BibleComponent component, AfterInteractEvent args)
    {
        if (!args.CanReach)
            return;

        if (!TryComp<UseDelayComponent>(uid, out var useDelay) || _delay.IsDelayed((uid, useDelay)))
            return;

        if (args.Target == null || args.Target == args.User || !_mobStateSystem.IsAlive(args.Target.Value))
        {
            return;
        }

        var target = args.Target.Value;
        var user = args.User;

        // Now chaplain is the only one who can interact with bible
        if (!TryComp<ChaplainComponent>(user, out var chaplain))
        {
            _popupSystem.PopupEntity(Loc.GetString("bible-sizzle"), user, user);

            _audio.PlayPvs(component.SizzleSoundPath, user);
            _damageableSystem.TryChangeDamage(user, component.DamageOnUntrainedUse, true, origin: uid);
            _delay.TryResetDelay((uid, useDelay));

            return;
        }

        // Tries to use chaplain's energy
        if (!_chaplain.TryUseAbility(user, chaplain, component.HealCost))
        {
            var othersFailMessage = Loc.GetString(component.LocPrefix + "-heal-fail-others", ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(target, EntityManager)), ("bible", uid));
            _popupSystem.PopupEntity(othersFailMessage, user, Filter.PvsExcept(user), true, PopupType.SmallCaution);

            var selfFailMessage = Loc.GetString(component.LocPrefix + "-heal-fail-self", ("target", Identity.Entity(target, EntityManager)), ("bible", uid));
            _popupSystem.PopupEntity(selfFailMessage, user, user, PopupType.MediumCaution);

            _audio.PlayPvs("/Audio/Effects/hit_kick.ogg", user);
            _damageableSystem.TryChangeDamage(target, component.DamageOnFail, true, origin: uid);
            _delay.TryResetDelay((uid, useDelay));

<<<<<<< HEAD
            return;
=======
            if (!HasComp<BibleUserComponent>(args.User))
            {
                _popupSystem.PopupEntity(Loc.GetString("bible-sizzle"), args.User, args.User);

                _audio.PlayPvs(component.SizzleSoundPath, args.User);
                _damageableSystem.TryChangeDamage(args.User, component.DamageOnUntrainedUse, true, origin: uid);
                _delay.TryResetDelay((uid, useDelay));

                return;
            }

            var userEnt = Identity.Entity(args.User, EntityManager);
            var targetEnt = Identity.Entity(args.Target.Value, EntityManager);

            // This only has a chance to fail if the target is not wearing anything on their head and is not a familiar.
            if (!_invSystem.TryGetSlotEntity(args.Target.Value, "head", out _) && !HasComp<FamiliarComponent>(args.Target.Value))
            {
                if (_random.Prob(component.FailChance))
                {
                    var othersFailMessage = Loc.GetString(component.LocPrefix + "-heal-fail-others", ("user", userEnt), ("target", targetEnt), ("bible", uid));
                    _popupSystem.PopupEntity(othersFailMessage, args.User, Filter.PvsExcept(args.User), true, PopupType.SmallCaution);

                    var selfFailMessage = Loc.GetString(component.LocPrefix + "-heal-fail-self", ("target", targetEnt), ("bible", uid));
                    _popupSystem.PopupEntity(selfFailMessage, args.User, args.User, PopupType.MediumCaution);

                    _audio.PlayPvs(component.BibleHitSound, args.User);
                    _damageableSystem.TryChangeDamage(args.Target.Value, component.DamageOnFail, true, origin: uid);
                    _delay.TryResetDelay((uid, useDelay));
                    return;
                }
            }

            string othersMessage;
            string selfMessage;

            if (_damageableSystem.TryChangeDamage(args.Target.Value, component.Damage, true, origin: uid))
            {
                othersMessage = Loc.GetString(component.LocPrefix + "-heal-success-others", ("user", userEnt), ("target", targetEnt), ("bible", uid));
                selfMessage = Loc.GetString(component.LocPrefix + "-heal-success-self", ("target", targetEnt), ("bible", uid));

                _audio.PlayPvs(component.HealSoundPath, args.User);
                _delay.TryResetDelay((uid, useDelay));

                if (component.HealingLightEffect.HasValue)
                    Spawn(component.HealingLightEffect.Value, new EntityCoordinates(args.Target.Value, default));
            }
            else
            {
                othersMessage = Loc.GetString(component.LocPrefix + "-heal-success-none-others", ("user", userEnt), ("target", targetEnt), ("bible", uid));
                selfMessage = Loc.GetString(component.LocPrefix + "-heal-success-none-self", ("target", targetEnt), ("bible", uid));
            }

            _popupSystem.PopupEntity(othersMessage, args.User, Filter.PvsExcept(args.User), true, PopupType.Medium);
            _popupSystem.PopupEntity(selfMessage, args.User, args.User, PopupType.Large);
        }

        var damage = _damageableSystem.TryChangeDamage(target, component.Damage, true, origin: uid);

        if (!damage)
        {
            var othersMessage = Loc.GetString(component.LocPrefix + "-heal-success-none-others", ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(target, EntityManager)), ("bible", uid));
            _popupSystem.PopupEntity(othersMessage, user, Filter.PvsExcept(user), true, PopupType.Medium);

            var selfMessage = Loc.GetString(component.LocPrefix + "-heal-success-none-self", ("target", Identity.Entity(target, EntityManager)), ("bible", uid));
            _popupSystem.PopupEntity(selfMessage, user, user, PopupType.Large);
        }
        else
        {
            var othersMessage = Loc.GetString(component.LocPrefix + "-heal-success-others", ("user", Identity.Entity(user, EntityManager)), ("target", Identity.Entity(target, EntityManager)), ("bible", uid));
            _popupSystem.PopupEntity(othersMessage, user, Filter.PvsExcept(user), true, PopupType.Medium);

            var selfMessage = Loc.GetString(component.LocPrefix + "-heal-success-self", ("target", Identity.Entity(target, EntityManager)), ("bible", uid));
            _popupSystem.PopupEntity(selfMessage, user, user, PopupType.Large);
            _audio.PlayPvs(component.HealSoundPath, user);
            _delay.TryResetDelay((uid, useDelay));
        }
    }

    private void AddSummonVerb(EntityUid uid, SummonableComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || component.AlreadySummoned || component.SpecialItemPrototype == null)
            return;

        if (component.RequiresBibleUser && !HasComp<ChaplainComponent>(args.User))
            return;

        AlternativeVerb verb = new()
        {
            Act = () =>
            {
                if (!TryComp<TransformComponent>(args.User, out var userXform))
                    return;

                AttemptSummon((uid, component), args.User, userXform);
            },
            Text = Loc.GetString("bible-summon-verb"),
            Priority = 2
        };
        args.Verbs.Add(verb);
    }

    public void AttemptSummon(Entity<SummonableComponent> ent, EntityUid user, TransformComponent? xForm)
    {
        var (uid, component) = ent;
        if (component.AlreadySummoned || component.SpecialItemPrototype == null)
            return;
        if (component.RequiresBibleUser && !HasComp<ChaplainComponent>(user))
            return;
        if (component.RequiresBibleUser && TryComp<ChaplainComponent>(user, out var chaplain) && !_chaplain.TryUseAbility(user, chaplain, component.SummonCost))
            return;
        if (!Resolve(user, ref xForm))
            return;
        if (component.Deleted || Deleted(uid))
            return;
        if (!_blocker.CanInteract(user, uid))
            return;

        // Make this familiar the component's summon
        var familiar = EntityManager.SpawnEntity(component.SpecialItemPrototype, xForm.Coordinates);
        component.Summon = familiar;
        component.PersonSummoned = user;

        // If this is going to use a ghost role mob spawner, attach it to the bible.
        if (HasComp<GhostRoleMobSpawnerComponent>(familiar))
        {
            _popupSystem.PopupEntity(Loc.GetString("bible-summon-requested"), user, PopupType.Medium);
            _transform.SetParent(familiar, uid);
        }
        component.AlreadySummoned = true;
    }

    private void OnSpawned(EntityUid uid, FamiliarComponent component, GhostRoleSpawnerUsedEvent args)
    {
        var parent = Transform(args.Spawner).ParentUid;
        if (!TryComp<SummonableComponent>(parent, out var summonable))
            return;

        component.Source = parent;
        summonable.Summon = uid;
    }

    private void OnFamiliarDeath(EntityUid uid, FamiliarComponent component, MobStateChangedEvent args)
    {
        if (component.Source == null)
            return;
        if (!TryComp<SummonableComponent>(component.Source, out var summonable))
            return;
        if (summonable.PersonSummoned != null)
            _popupSystem.PopupEntity(Loc.GetString("bible-familiar-dead"), summonable.PersonSummoned.Value, PopupType.SmallCaution);
        summonable.AlreadySummoned = false;
    }
}
