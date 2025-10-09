using Content.Server.ADT.Salvage.Components;
using Content.Server.Medical;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Pinpointer;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Content.Shared.Warps;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using Robust.Shared.Spawners;

namespace Content.Server.ADT.Salvage.Systems;

public sealed partial class JaunterPortalSystem : EntitySystem
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

        SubscribeLocalEvent<JaunterPortalComponent, StartCollideEvent>(OnPortalEntered);
        SubscribeLocalEvent<JaunterKillPortalComponent, StartCollideEvent>(OnKillPortalEntered);
        SubscribeLocalEvent<JaunterPortalComponent, MapInitEvent>(OnLinkedPortalSpawn);
    }

    private void OnKillPortalEntered(EntityUid uid, JaunterKillPortalComponent comp, ref StartCollideEvent args)
    {
        if (args.OtherEntity == default)
            return;

        QueueDel(args.OtherEntity);
    }

    public EntityUid? SpawnLinkedPortal(EntityUid uid)
    {
        var at = Transform(uid).Coordinates;
        var spawnAt = FindFreeNearbyCoords(at, 2) ?? at;
        var portal = Spawn("ADTJaunterPortal", spawnAt);

        var td = EnsureComp<TimedDespawnComponent>(portal);
        td.Lifetime = InitialLifetime;

        return portal;
    }

    public void OnLinkedPortalSpawn(EntityUid uid, JaunterPortalComponent comp, MapInitEvent args)
    {
        var dest = GetRandomBeacon();
        if (dest == null)
        {
            SpawnKillPortal(uid);
            return;
        }

        if (TryComp<PortalComponent>(uid, out var portalComp))
            portalComp.CanTeleportToOtherMaps = true;

        _link.OneWayLink(uid, dest.Value, deleteOnEmptyLinks: false);
    }

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

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/hiss.ogg"), subject);
        return true;
    }

    public EntityUid? GetRandomBeacon()
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

    public EntityUid SpawnKillPortal(EntityUid uid)
    {
        var at = Transform(uid).Coordinates;
        var spawnAt = FindFreeNearbyCoords(at, 2) ?? at;
        var portal = Spawn("ADTJaunterBlackKillPortal", spawnAt);
        QueueDel(uid);

        return portal;
    }

    private EntityCoordinates? FindFreeNearbyCoords(EntityCoordinates origin, int radius)
    {
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
        var otherUid = args.OtherEntity;

        if (otherUid == default)
            return;

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Items/Mining/fultext_launch.ogg"), args.OtherEntity);

        if (TryComp<StaminaComponent>(otherUid, out var stam))
        {
            var need = MathF.Max(0.01f, stam.CritThreshold - stam.StaminaDamage);
            _stamina.TakeStaminaDamage(otherUid, need, stam);
        }

        if (HasComp<BodyComponent>(otherUid) && HasComp<HungerComponent>(otherUid))
        {
            _vomit.Vomit(otherUid);
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
