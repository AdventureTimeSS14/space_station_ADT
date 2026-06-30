using Content.Server.Fluids.EntitySystems;
using Content.Server.Objectives.Components;
using Content.Server.Objectives.Systems;
using Content.Server.Popups;
using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Content.Shared.Dragon;
using Content.Shared.Gibbing;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Zombies;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems; // ADT-Tweak
// ADT-Tweak-Start
using Content.Shared.Sprite;
using Content.Server.Stunnable;
using Content.Shared.Devour.Components;
using Content.Shared.NPC.Components;
using Robust.Shared.Serialization.Manager;
using Content.Server.Body.Systems;
// ADT-Tweak-End

namespace Content.Server.Dragon;

public sealed class DragonSystem : EntitySystem
{
    [Dependency] private readonly CarpRiftsConditionSystem _carpRifts = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;
    // ADT-Tweak-Start
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly ISerializationManager _serManager = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    // ADT-Tweak-End

    private EntityQuery<CarpRiftsConditionComponent> _objQuery;

    /// <summary>
    /// Minimum distance between 2 rifts allowed.
    /// </summary>
    private const int RiftRange = 15;

    /// <summary>
    /// Radius of tiles
    /// </summary>
    private const int RiftTileRadius = 2;

    private const int RiftsAllowed = 3;

    public override void Initialize()
    {
        base.Initialize();

        _objQuery = GetEntityQuery<CarpRiftsConditionComponent>();

        SubscribeLocalEvent<DragonComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<DragonComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DragonComponent, DragonSpawnRiftActionEvent>(OnSpawnRift);
        SubscribeLocalEvent<DragonComponent, RefreshMovementSpeedModifiersEvent>(OnDragonMove);
        SubscribeLocalEvent<DragonComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<DragonComponent, EntityZombifiedEvent>(OnZombified);
        // ADT-Tweak-Start
        SubscribeLocalEvent<DragonComponent, DragonRoarActionEvent>(OnDragonRoar);
        SubscribeLocalEvent<DragonComponent, DragonSpawnCarpHordeActionEvent>(OnRiseFish);
        // ADT-Tweak-End
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // ADT-Tweak-Start
        var query = EntityQueryEnumerator<DragonComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var comp, out var xform))
        {
            // Heal the dragon a bit if it's near the carp rift.
            if (_lookup.GetEntitiesInRange<DragonRiftComponent>(xform.Coordinates, comp.CarpRiftHealingRange).Count > 0)
                _damage.TryChangeDamage(uid, comp.CarpRiftHealing * frameTime, true, false);
            // ADT-Tweak-End

            if (comp.WeakenedAccumulator > 0f)
            {
                comp.WeakenedAccumulator -= frameTime;

                // No longer weakened.
                if (comp.WeakenedAccumulator < 0f)
                {
                    comp.WeakenedAccumulator = 0f;
                    _movement.RefreshMovementSpeedModifiers(uid);
                }
            }

            // At max rifts
            if (comp.Rifts.Count >= RiftsAllowed)
                continue;

            // If there's an active rift don't accumulate.
            if (comp.Rifts.Count > 0)
            {
                var lastRift = comp.Rifts[^1];

                if (TryComp<DragonRiftComponent>(lastRift, out var rift) && rift.State != DragonRiftState.Finished)
                {
                    comp.RiftAccumulator = 0f;
                    continue;
                }
            }

            if (!_mobState.IsDead(uid))
                comp.RiftAccumulator += frameTime;

            // Gib it, naughty dragon!
            if (comp.RiftAccumulator >= comp.RiftMaxAccumulator)
            {
                Roar(uid, comp, Transform(uid).Coordinates);
                var smoke = Spawn(comp.SmokePrototype, Transform(uid).Coordinates);
                if (TryComp<SmokeComponent>(smoke, out var smokeComp))
                    _smoke.StartSmoke(smoke, comp.SmokeSolution, smokeComp.Duration, smokeComp.SpreadAmount, smokeComp);
                _gibbing.Gib(uid);
            }
        }
    }

    private void OnInit(EntityUid uid, DragonComponent component, MapInitEvent args)
    {
        Roar(uid, component);
        
        _actions.AddAction(uid, ref component.SpawnRiftActionEntity, component.SpawnRiftAction);
        // ADT-Tweak-Start
        _actions.AddAction(uid, ref component.SpawnCarpsActionEntity, component.SpawnCarpsAction);
        _actions.AddAction(uid, ref component.RoarActionEntity, component.RoarAction);
        // ADT-Tweak-End
    }

    private void OnShutdown(EntityUid uid, DragonComponent component, ComponentShutdown args)
    {
        DeleteRifts(uid, false, component);
        
        _actions.RemoveAction(uid, component.SpawnRiftActionEntity);
        // ADT-Tweak-Start
        _actions.RemoveAction(uid, component.SpawnCarpsActionEntity);
        _actions.RemoveAction(uid, component.RoarActionEntity);
        // ADT-Tweak-End
    }

    private void OnSpawnRift(EntityUid uid, DragonComponent component, DragonSpawnRiftActionEvent args)
    {
        if (component.Weakened)
        {
            _popup.PopupEntity(Loc.GetString("carp-rift-weakened"), uid, uid);
            return;
        }

        if (component.Rifts.Count >= RiftsAllowed)
        {
            _popup.PopupEntity(Loc.GetString("carp-rift-max"), uid, uid);
            return;
        }

        if (component.Rifts.Count > 0 && TryComp<DragonRiftComponent>(component.Rifts[^1], out var rift) && rift.State != DragonRiftState.Finished)
        {
            _popup.PopupEntity(Loc.GetString("carp-rift-duplicate"), uid, uid);
            return;
        }

        var xform = Transform(uid);

        // Have to be on a grid fam
        if (!TryComp<MapGridComponent>(xform.GridUid, out var grid))
        {
            _popup.PopupEntity(Loc.GetString("carp-rift-anchor"), uid, uid);
            return;
        }

        // cant stack rifts near eachother
        foreach (var (_, riftXform) in EntityQuery<DragonRiftComponent, TransformComponent>(true))
        {
            if (_transform.InRange(riftXform.Coordinates, xform.Coordinates, RiftRange))
            {
                _popup.PopupEntity(Loc.GetString("carp-rift-proximity", ("proximity", RiftRange)), uid, uid);
                return;
            }
        }

        // cant put a rift on solars
        foreach (var tile in _map.GetTilesIntersecting(xform.GridUid.Value, grid, new Circle(_transform.GetWorldPosition(xform), RiftTileRadius), false))
        {
            if (!_turf.IsSpace(tile))
                continue;

            _popup.PopupEntity(Loc.GetString("carp-rift-space-proximity", ("proximity", RiftTileRadius)), uid, uid);
            return;
        }

        var carpUid = Spawn(component.RiftPrototype, _transform.GetMapCoordinates(uid, xform: xform));
        Transform(carpUid).LocalRotation = Angle.Zero;

        component.Rifts.Add(carpUid);
        Comp<DragonRiftComponent>(carpUid).Dragon = uid;
    }

    // TODO: just make this a move speed modifier component???
    private void OnDragonMove(EntityUid uid, DragonComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.Weakened)
        {
            args.ModifySpeed(0.5f, 0.5f);
        }
    }

    private void OnMobStateChanged(EntityUid uid, DragonComponent component, MobStateChangedEvent args)
    {
        // Deletes all rifts after dying
        if (args.NewMobState != MobState.Dead)
            return;

        if (component.SoundDeath != null)
            _audio.PlayPvs(component.SoundDeath, uid);

        // objective is explicitly not reset so that it will show how many you got before dying in round end text
        DeleteRifts(uid, false, component);
    }

    private void OnZombified(Entity<DragonComponent> ent, ref EntityZombifiedEvent args)
    {
        // prevent carp attacking zombie dragon
        _faction.AddFaction(ent.Owner, ent.Comp.Faction);
    }

    private void Roar(EntityUid uid, DragonComponent comp, EntityCoordinates? coords = null)
    {
        if (comp.SoundRoar != null)
        {
            if (coords != null)
                _audio.PlayPvs(comp.SoundRoar, coords.Value);
            else if (HasComp<TransformComponent>(uid))
                _audio.PlayPvs(comp.SoundRoar, uid);
        }
    }

    /// <summary>
    /// Delete all rifts this dragon made.
    /// </summary>
    /// <param name="uid">Entity id of the dragon</param>
    /// <param name="resetRole">If true, the role's rift count will be reset too</param>
    /// <param name="comp">The dragon component</param>
    public void DeleteRifts(EntityUid uid, bool resetRole, DragonComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        foreach (var rift in comp.Rifts)
        {
            QueueDel(rift);
        }

        comp.Rifts.Clear();

        // stop here if not trying to reset the objective's rift count
        if (!resetRole || !_mind.TryGetMind(uid, out _, out var mind))
            return;

        foreach (var objId in mind.Objectives)
        {
            if (_objQuery.TryGetComponent(objId, out var obj))
            {
                _carpRifts.ResetRifts(objId, obj);
                break;
            }
        }
    }

    /// <summary>
    /// Increment the dragon role's charged rift count.
    /// </summary>
    public void RiftCharged(EntityUid uid, DragonComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        if (!_mind.TryGetMind(uid, out _, out var mind))
            return;

        foreach (var objId in mind.Objectives)
        {
            if (_objQuery.TryGetComponent(objId, out var obj))
            {
                _carpRifts.RiftCharged(objId, obj);
                break;
            }
        }
    }

    /// <summary>
    /// Do everything that needs to happen when a rift gets destroyed by the crew.
    /// </summary>
    public void RiftDestroyed(EntityUid uid, DragonComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return;

        // ADT-Tweak-Start
        // do reset the rift count since crew destroyed the rift, not deleted by the dragon dying.
        DeleteRifts(uid, true, comp);
        // ADT-Tweak-End

        // We can't predict the rift being destroyed anyway so no point adding weakened to shared.
        comp.WeakenedAccumulator = comp.WeakenedDuration;
        _movement.RefreshMovementSpeedModifiers(uid);
        _popup.PopupEntity(Loc.GetString("carp-rift-destroyed"), uid, uid);
    }

    // ADT-Tweak-Start
    private void OnRiseFish(EntityUid uid, DragonComponent component, DragonSpawnCarpHordeActionEvent args)
    {
        if (args.Handled)
            return;

        Roar(uid, component);
        var xform = Transform(uid);
        for (int i = 0; i < component.CarpAmount; i++)
        {
            var ent = Spawn(component.CarpProtoId, xform.Coordinates);

            // Update their look to match the leader.
            if (TryComp<RandomSpriteComponent>(uid, out var randomSprite))
            {
                var spawnedSprite = EnsureComp<RandomSpriteComponent>(ent);
                _serManager.CopyTo(randomSprite, ref spawnedSprite, notNullableOverride: true);
                Dirty(ent, spawnedSprite);
            }
        }

        args.Handled = true;
    }

    private void OnDragonRoar(EntityUid uid, DragonComponent component, DragonRoarActionEvent args)
    {
        if (args.Handled)
            return;

        Roar(uid, component);

        // TODO: add pushing (like from push horn but stronger) after upstream is merged

        var xform = Transform(uid);
        var nearMobs = _lookup.GetEntitiesInRange<NpcFactionMemberComponent>(xform.Coordinates, component.RoarRange, LookupFlags.Uncontained);
        foreach (var mob in nearMobs)
        {
            if (_faction.IsEntityFriendly(uid, (mob.Owner, mob.Comp)))
                continue;

            _stun.TryUpdateStunDuration (mob, TimeSpan.FromSeconds(component.RoarStunTime));
        }

        args.Handled = true;
    }
    // ADT-Tweak-End
}
