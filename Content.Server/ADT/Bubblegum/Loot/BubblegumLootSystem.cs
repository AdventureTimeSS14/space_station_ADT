using System.Numerics;
using Content.Server.Storage.EntitySystems;
using Content.Shared.ADT.Bubblegum;
using Content.Shared.Camera;
using Content.Shared.Flash;
using Content.Shared.Mobs;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Bubblegum;

public sealed class BubblegumLootSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly SharedFlashSystem _flash = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BubblegumLootComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<BubblegumLootComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (TerminatingOrDeleted(ent.Owner))
            return;

        if (ent.Comp.LootDropped || ent.Comp.DespawnAt != null)
            return;

        var now = _timing.CurTime;
        ent.Comp.DespawnAt = now + ent.Comp.DespawnDelay;
        ent.Comp.NextBloodBeatAt = now + ent.Comp.BloodBeatInterval;

        StartCinematic(ent);
    }

    private void StartCinematic(Entity<BubblegumLootComponent> ent)
    {
        if (ent.Comp.SequenceStarted)
            return;

        ent.Comp.SequenceStarted = true;

        _audio.PlayPvs(ent.Comp.DeathBoomSound, ent);
        _audio.PlayPvs(ent.Comp.DeathRoarSound, ent);
        _audio.PlayPvs(ent.Comp.DeathThemeSound, ent);

        SpawnAtCoords(ent.Comp.DeathGlowProto, Transform(ent).Coordinates);
        ShakeNearby(ent, ent.Comp.ShakeStrength);

        _flash.FlashArea(ent.Owner, null, ent.Comp.ShakeRange, ent.Comp.ImpactSlowDuration,
            slowTo: ent.Comp.ImpactSlowTo, displayPopup: false);

        EnsureComp<BubblegumDeathVisualsComponent>(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BubblegumLootComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.LootDropped || comp.DespawnAt == null)
                continue;

            var now = _timing.CurTime;

            var remaining = (comp.DespawnAt.Value - now).TotalSeconds;
            var progress = (float)Math.Clamp(1 - remaining / comp.DespawnDelay.TotalSeconds, 0f, 1f);

            if (now >= comp.NextBloodBeatAt && now < comp.DespawnAt.Value)
            {
                var interval = comp.BloodBeatInterval * (1f - 0.6f * progress);
                comp.NextBloodBeatAt = now + interval;
                SpawnBloodRing((uid, comp));
                ShakeNearby((uid, comp), comp.ShakeStrength * (0.3f + 1.2f * progress));
            }

            if (!comp.SoulReleased && now >= comp.DespawnAt.Value - comp.ImplosionLead)
                ReleaseSoul((uid, comp));

            if (now >= comp.DespawnAt.Value)
                Finale((uid, comp));
        }
    }

    private void ReleaseSoul(Entity<BubblegumLootComponent> ent)
    {
        ent.Comp.SoulReleased = true;

        _audio.PlayPvs(ent.Comp.ImplosionSound, ent);
        SpawnAtCoords(ent.Comp.DemonSoulProto, Transform(ent).Coordinates);
        SpawnBloodRing(ent);
    }

    private void Finale(Entity<BubblegumLootComponent> ent)
    {
        ShakeNearby(ent, ent.Comp.ShakeStrength * 2f);
        _flash.FlashArea(ent.Owner, null, ent.Comp.ShakeRange, ent.Comp.FlashDuration, slowTo: 1f);
        SpawnAtCoords(ent.Comp.DeathFlashProto, Transform(ent).Coordinates);
        _audio.PlayPvs(ent.Comp.DissolveSound, ent);

        DropLoot(ent);
    }

    private void DropLoot(Entity<BubblegumLootComponent> ent)
    {
        ent.Comp.LootDropped = true;

        var coords = Transform(ent).Coordinates;
        var chest = Spawn(ent.Comp.ChestProto, coords);

        foreach (var (proto, maxAmount) in ent.Comp.RandomAmountLoot)
        {
            var amount = _random.Next(1, maxAmount + 1);
            SpawnAmountIntoChest(chest, proto, amount, coords);
        }

        if (ent.Comp.RandomLoot.Count > 0)
        {
            var picked = _random.Pick(ent.Comp.RandomLoot);
            SpawnIntoChest(chest, picked, coords);
        }

        foreach (var proto in ent.Comp.GuaranteedLoot)
        {
            SpawnIntoChest(chest, proto, coords);
        }

        SpawnAtCoords(ent.Comp.ChestGlowProto, Transform(chest).Coordinates);
        _audio.PlayPvs(ent.Comp.ChestRewardSound, chest);

        QueueDel(ent.Owner);
    }

    private void SpawnBloodRing(Entity<BubblegumLootComponent> ent)
    {
        var center = Transform(ent).Coordinates;

        var normalBlood = "ADTPuddleBloodBubblegum";
        var thickBlood = "ADTPuddleBloodBubblegumThick";
        if (TryComp<BubblegumComponent>(ent, out var bubblegum))
        {
            normalBlood = bubblegum.BloodPrototype;
            thickBlood = bubblegum.ThickBloodPrototype;
        }

        var startAngle = _random.NextFloat(0f, MathF.Tau);
        for (var i = 0; i < ent.Comp.BloodRingCount; i++)
        {
            var angle = startAngle + MathF.Tau / ent.Comp.BloodRingCount * i;
            var dist = ent.Comp.BloodRingRadius * _random.NextFloat(0.7f, 1.1f);
            var offset = new Vector2(MathF.Cos(angle) * dist, MathF.Sin(angle) * dist);

            var proto = _random.Prob(0.5f) ? thickBlood : normalBlood;
            SpawnAtCoords(proto, center.Offset(offset));
        }
    }

    private void ShakeNearby(Entity<BubblegumLootComponent> ent, float strength)
    {
        var mapCoords = _transform.GetMapCoordinates(ent);
        if (mapCoords.MapId == MapId.Nullspace)
            return;

        var recoils = new HashSet<Entity<CameraRecoilComponent>>();
        _lookup.GetEntitiesInRange(mapCoords, ent.Comp.ShakeRange, recoils);

        foreach (var recoil in recoils)
        {
            var kick = new Vector2(_random.NextFloat(-strength, strength), _random.NextFloat(-strength, strength));
            _recoil.KickCamera(recoil, kick);
        }
    }

    private void SpawnAmountIntoChest(EntityUid chest, string proto, int amount, EntityCoordinates fallback)
    {
        var spawned = SpawnIntoChest(chest, proto, fallback);
        if (spawned == null)
            return;

        if (TryComp<StackComponent>(spawned.Value, out var stack))
        {
            _stack.SetCount((spawned.Value, stack), amount);
            return;
        }

        for (var i = 1; i < amount; i++)
        {
            SpawnIntoChest(chest, proto, fallback);
        }
    }

    private EntityUid? SpawnIntoChest(EntityUid chest, string proto, EntityCoordinates fallback)
    {
        var item = Spawn(proto, fallback);
        if (!_entityStorage.Insert(item, chest))
            _transform.SetCoordinates(item, fallback);

        return item;
    }

    private EntityUid? SpawnAtCoords(string proto, EntityCoordinates coords)
    {
        if (!coords.IsValid(EntityManager))
            return null;

        return Spawn(proto, coords);
    }
}
