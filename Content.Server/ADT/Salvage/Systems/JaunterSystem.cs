using Content.Server.ADT.Salvage.Components;
using Content.Server.Medical;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Pinpointer;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Warps;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Server.ADT.Salvage.Systems;

/// <summary>
///     Handles using the miner's jaunter to spawn a shadow-style portal
///     that links to a random station beacon (warp point) and despawns
///     after 15 seconds idle or 3 seconds after first entry.
/// </summary>
public sealed class JaunterSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly LinkedEntitySystem _link = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly VomitSystem _vomit = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private const float InitialLifetime = 15f;
    private const float AfterEnterLifetime = 3f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<JaunterComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<JaunterPortalComponent, StartCollideEvent>(OnPortalEntered);
        SubscribeLocalEvent<JaunterKillPortalComponent, StartCollideEvent>(OnKillPortalEntered);
    }

    private void OnUseInHand(EntityUid uid, JaunterComponent comp, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        SpawnLinkedPortal(args.User);
        QueueDel(uid);
        args.Handled = true;
    }

    private void OnKillPortalEntered(EntityUid uid, JaunterKillPortalComponent comp, ref StartCollideEvent args)
    {
        if (args.OtherEntity == default)
            return;

        // Delete the entering entity.
        QueueDel(args.OtherEntity);
    }

    /// <summary>
    ///     Spawns a shadow portal at the user's feet and links it one-way to a random station beacon.
    /// </summary>
    public EntityUid? SpawnLinkedPortal(EntityUid user)
    {
        var dest = GetRandomBeacon();
        if (dest == null)
        {
            SpawnKillPortal(user);
            return null;
        }

        var at = Transform(user).Coordinates;
        var spawnAt = FindFreeNearbyCoords(user, at, 2) ?? at;
        var portal = Spawn("ADTJaunterPortal", spawnAt);

        // mark as jaunter portal and set initial lifetime
        var jp = EnsureComp<JaunterPortalComponent>(portal);
        var td = EnsureComp<TimedDespawnComponent>(portal);
        td.Lifetime = InitialLifetime;

        // ensure portal can teleport across maps if needed
        if (TryComp<PortalComponent>(portal, out var portalComp))
            portalComp.CanTeleportToOtherMaps = true;

        _link.OneWayLink(portal, dest.Value, deleteOnEmptyLinks: false);
        return portal;
    }

    /// <summary>
    ///     Teleports an entity directly to a random station beacon, without spawning a portal.
    /// </summary>
    public bool TeleportToRandomBeacon(EntityUid subject)
    {
        var dest = GetRandomBeacon();
        if (dest == null)
            return false;

        var targetCoords = Transform(dest.Value).Coordinates;
        _transform.SetCoordinates(subject, targetCoords);
        _transform.AttachToGridOrMap(subject, Transform(subject));
        return true;
    }

    /// <summary>
    ///     Teleports the subject to a random station beacon while playing shadow-portal SFX at source and destination.
    /// </summary>
    public bool TeleportToRandomBeaconWithFx(EntityUid subject)
    {
        var dest = GetRandomBeacon();
        if (dest == null)
            return false;

        var sourceCoords = Transform(subject).Coordinates;
        var targetCoords = Transform(dest.Value).Coordinates;

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/hiss.ogg"), subject);

        _transform.SetCoordinates(subject, targetCoords);
        _transform.AttachToGridOrMap(subject, Transform(subject));

        // play at destination as well
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/hiss.ogg"), subject);
        return true;
    }

    private EntityUid? GetRandomBeacon()
    {
        var candidates = new List<EntityUid>();
        var query = EntityQueryEnumerator<WarpPointComponent, NavMapBeaconComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var warp, out var beacon, out var xform))
        {
            if (!beacon.Enabled)
                continue;
            if (xform.MapUid == null)
                continue;
            candidates.Add(uid);
        }

        if (candidates.Count == 0)
            return null;

        return _random.Pick(candidates);
    }

    public EntityUid SpawnKillPortal(EntityUid user)
    {
        var at = Transform(user).Coordinates;
        var spawnAt = FindFreeNearbyCoords(user, at, 2) ?? at;
        var portal = Spawn("ADTJaunterBlackKillPortal", spawnAt);
        return portal;
    }

    private EntityCoordinates? FindFreeNearbyCoords(EntityUid user, EntityCoordinates origin, int radius)
    {
        // Try a shuffled set of offsets within the radius; fall back to null if none found
        var offsets = new List<(int dx, int dy)>();
        for (var dy = -radius; dy <= radius; dy++)
        {
            for (var dx = -radius; dx <= radius; dx++)
            {
                if (dx == 0 && dy == 0)
                    continue;
                if (dx * dx + dy * dy > radius * radius)
                    continue;
                offsets.Add((dx, dy));
            }
        }

        _random.Shuffle(offsets);

        foreach (var (dx, dy) in offsets)
        {
            var candidate = new EntityCoordinates(origin.EntityId, origin.X + dx, origin.Y + dy);

            // consider a small radius to check occupancy
            var occupied = false;
            foreach (var ent in _lookup.GetEntitiesInRange(candidate, 0.2f))
            {
                if (!TryComp<PhysicsComponent>(ent, out var phys))
                    continue;
                if (phys.CanCollide)
                {
                    occupied = true;
                    break;
                }
            }

            if (occupied)
                continue;

            return candidate;
        }

        return null;
    }

    private void OnPortalEntered(EntityUid uid, JaunterPortalComponent comp, ref StartCollideEvent args)
    {
        // Only react to non-projectile portal collisions handled by portal system
        if (args.OtherEntity == default)
            return;

        // When first entity enters, shorten lifetime to 3 seconds
        if (comp.EnteredOnce)
            return;

        comp.EnteredOnce = true;

        // Apply effects to the entering entity: fall teleport sound, stamina crit, vomit
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/Mining/fultext_launch.ogg"), args.OtherEntity);

        if (TryComp<StaminaComponent>(args.OtherEntity, out var stam))
        {
            // Push over crit threshold
            var need = MathF.Max(0.01f, stam.CritThreshold - stam.StaminaDamage);
            _stamina.TakeStaminaDamage(args.OtherEntity, need, stam);
        }

        if (HasComp<BodyComponent>(args.OtherEntity) && HasComp<HungerComponent>(args.OtherEntity))
        {
            _vomit.Vomit(args.OtherEntity);
        }

        if (TryComp<TimedDespawnComponent>(uid, out var td))
        {
            td.Lifetime = MathF.Min(td.Lifetime, AfterEnterLifetime);
        }
        else
        {
            var ntd = EnsureComp<TimedDespawnComponent>(uid);
            ntd.Lifetime = AfterEnterLifetime;
        }
    }
}




