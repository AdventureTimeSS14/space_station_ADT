using Content.Server.Audio;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Power.Components;
using Content.Shared.ADT.Artillery;
using Content.Shared.Audio;
using Content.Shared.Camera;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Server.ADT.Artillery;

public sealed class ArtilleryCannonSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ArtilleryCannonComponent, ComponentInit>(OnCannonInit);
        SubscribeLocalEvent<ArtilleryCannonComponent, ArtilleryCannonFireEvent>(OnCannonFire);
    }

    private void OnCannonInit(EntityUid uid, ArtilleryCannonComponent component, ComponentInit args)
    {
        if (TryComp<PowerConsumerComponent>(uid, out var powerConsumer))
        {
            powerConsumer.DrawRate = 0;
        }
    }

    public override void Update(float frameTime)
    {
        var cannonQuery = EntityQueryEnumerator<ArtilleryCannonComponent, PowerConsumerComponent>();
        while (cannonQuery.MoveNext(out var uid, out var cannon, out var powerConsumer))
        {
            var newDrawRate = cannon.IsEnabled ? cannon.PowerUseActive : 0;
            powerConsumer.DrawRate = newDrawRate;

            var fullyPowered = cannon.IsEnabled && powerConsumer.ReceivedPower >= cannon.PowerUseActive;
            var isReloading = cannon.NextFireTime > _timing.CurTime;

            if (isReloading && !cannon.IsCharging && !fullyPowered)
            {
                cannon.NextFireTime += TimeSpan.FromSeconds(frameTime);
            }

            if (TryComp<AppearanceComponent>(uid, out _))
            {
                var showReloading = isReloading && !cannon.IsCharging && fullyPowered;
                var showPowered = fullyPowered && !cannon.IsCharging && !isReloading;

                _appearance.SetData(uid, ArtilleryVisuals.Charging, cannon.IsCharging);
                _appearance.SetData(uid, ArtilleryVisuals.Reloading, showReloading);
                _appearance.SetData(uid, PowerDeviceVisuals.Powered, showPowered);
            }

            if (TryComp<AmbientSoundComponent>(uid, out var ambientSound))
            {
                _ambientSound.SetAmbience(uid, fullyPowered, ambientSound);
            }
        }

        var shakeQuery = EntityQueryEnumerator<ArtilleryCameraShakeComponent>();
        var toRemove = new List<EntityUid>();
        while (shakeQuery.MoveNext(out var shakeUid, out var shake))
        {
            if (_timing.CurTime >= shake.EndTime)
            {
                toRemove.Add(shakeUid);
                continue;
            }

            if (!TryComp<ArtilleryCannonComponent>(shake.CannonUid, out var cannon))
            {
                toRemove.Add(shakeUid);
                continue;
            }

            var playerPos = _transform.GetWorldPosition(shakeUid);
            var delta = shake.EpicenterPosition - playerPos;
            var distance = delta.Length();

            if (distance > cannon.EffectRange)
            {
                toRemove.Add(shakeUid);
                continue;
            }

            if (_timing.CurTime - shake.LastShakeTime >= TimeSpan.FromSeconds(shake.ShakeInterval))
            {
                shake.LastShakeTime = _timing.CurTime;
                shake.ShakeCount++;

                var distanceFactor = 1f - distance / cannon.EffectRange;
                var intensity = shake.BaseIntensity * distanceFactor;

                var angle = _random.NextFloat() * MathF.PI * 2f;
                var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));

                _recoil.KickCamera(shakeUid, direction * intensity);
            }
        }

        foreach (var uid in toRemove)
        {
            RemCompDeferred<ArtilleryCameraShakeComponent>(uid);
        }
    }

    private void OnCannonFire(EntityUid cannonUid, ArtilleryCannonComponent cannon, ArtilleryCannonFireEvent args)
    {
        var beaconMapCoords = args.TargetMapCoords;
        cannon.IsCharging = true;

        if (TryComp<AppearanceComponent>(cannonUid, out _))
        {
            _appearance.SetData(cannonUid, ArtilleryVisuals.Charging, true);
            _appearance.SetData(cannonUid, ArtilleryVisuals.Reloading, false);
            _appearance.SetData(cannonUid, PowerDeviceVisuals.Powered, false);
        }

        _audio.PlayPvs(new SoundPathSpecifier(cannon.FireSound), cannonUid, AudioParams.Default.WithMaxDistance(cannon.EffectRange).WithVolume(5f));

        Timer.Spawn(TimeSpan.FromSeconds(4), () =>
        {
            if (!Exists(cannonUid))
                return;

            if (TryComp<ArtilleryCannonComponent>(cannonUid, out var cannonComp))
                cannonComp.IsCharging = false;

            FireLaser(cannonUid, cannon);
        });

        Timer.Spawn(TimeSpan.FromSeconds(6), () =>
        {
            if (!Exists(cannonUid))
                return;

            var message = Loc.GetString("artillery-fire-announcement");
            _chat.DispatchGlobalAnnouncement(message,
                Loc.GetString("artillery-announcement-sender"),
                playSound: false,
                colorOverride: Color.Red);
            _audio.PlayGlobal("/Audio/Corvax/Adminbuse/artillery.ogg", Filter.Broadcast(), true);
        });

        for (int i = 0; i < 8; i++)
        {
            var delay = 11.0 + (i * 0.5);
            Timer.Spawn(TimeSpan.FromSeconds(delay), () =>
            {
                if (!Exists(cannonUid))
                    return;

                SpawnBluespaceFlash(beaconMapCoords, cannon);
            });
        }

        Timer.Spawn(TimeSpan.FromSeconds(14.5), () =>
        {
            if (!Exists(cannonUid))
                return;

            var explosionCoords = new MapCoordinates(beaconMapCoords.Position, beaconMapCoords.MapId);
            EntityManager.SpawnEntity("BSAExplosion", explosionCoords);
        });

        Timer.Spawn(TimeSpan.FromSeconds(15), () =>
        {
            if (!Exists(cannonUid))
                return;

            _explosion.QueueExplosion(beaconMapCoords, cannon.ExplosionType, cannon.TotalIntensity, cannon.IntensitySlope, cannon.MaxIntensity, null);
            ApplyFireEffects(cannonUid, cannon, beaconMapCoords);
        });
    }

    private void SpawnBluespaceFlash(MapCoordinates center, ArtilleryCannonComponent cannon)
    {
        var radius = 10f;
        var angle = _random.NextFloat() * MathF.PI * 2f;
        var distance = _random.NextFloat() * radius;
        var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
        var flashPos = new MapCoordinates(center.Position + offset, center.MapId);
        EntityManager.SpawnEntity(cannon.BluespaceFlashProto, flashPos);
    }

    private void ApplyFireEffects(EntityUid cannonUid, ArtilleryCannonComponent cannon, MapCoordinates epicenter)
    {
        var filter = Filter.Empty().AddInRange(epicenter, cannon.EffectRange, _playerManager, EntityManager);
        foreach (var player in filter.Recipients)
        {
            if (player.AttachedEntity is not EntityUid uid)
                continue;

            var playerPos = _transform.GetWorldPosition(uid);
            var delta = epicenter.Position - playerPos;
            var distance = delta.Length();

            if (distance > cannon.EffectRange)
                continue;

            var shake = EnsureComp<ArtilleryCameraShakeComponent>(uid);
            shake.StartTime = _timing.CurTime;
            shake.EndTime = _timing.CurTime + TimeSpan.FromSeconds(shake.ShakeDuration);
            shake.LastShakeTime = _timing.CurTime;
            shake.EpicenterPosition = epicenter.Position;

            var distanceFactor = 1f - distance / cannon.EffectRange;
            var baseIntensity = shake.ShakeIntensity * distanceFactor;

            shake.BaseIntensity = baseIntensity;
            shake.CannonUid = cannonUid;
            shake.ShakeCount = 0;
        }
    }

    private void FireLaser(EntityUid cannonUid, ArtilleryCannonComponent cannon)
    {
        if (!TryComp(cannonUid, out TransformComponent? cannonXform))
            return;

        if (cannonXform.MapUid == null)
            return;

        var cannonPos = _transform.GetWorldPosition(cannonUid);

        var gridRotation = Angle.Zero;
        if (cannonXform.ParentUid != cannonXform.MapUid && TryComp<TransformComponent>(cannonXform.ParentUid, out var parentXform))
        {
            gridRotation = parentXform.WorldRotation;
        }

        var baseDirection = cannon.FireDirection.ToVec().Normalized();
        var directionVector = gridRotation.RotateVec(baseDirection);

        var cannonMapCoords = new MapCoordinates(cannonPos, Transform(cannonXform.MapUid.Value).MapID);
        ApplyFireEffects(cannonUid, cannon, cannonMapCoords);

        var baseMuzzleOffset = new Vector2(cannon.MuzzleForwardOffset, cannon.MuzzleVerticalOffset);
        var rotatedMuzzleOffset = gridRotation.RotateVec(baseMuzzleOffset);
        var startPos = cannonPos + rotatedMuzzleOffset;

        var mapId = Transform(cannonXform.MapUid.Value).MapID;
        var mapCoords = new MapCoordinates(startPos, mapId);

        var laserUid = EntityManager.SpawnEntity(cannon.LaserProto, mapCoords);

        var fromCoords = new EntityCoordinates(cannonXform.MapUid.Value, startPos);
        var hitscanEv = new HitscanTraceEvent
        {
            FromCoordinates = fromCoords,
            ShotDirection = directionVector.Normalized(),
            Gun = cannonUid,
            Shooter = null,
            Target = null,
        };

        RaiseLocalEvent(laserUid, ref hitscanEv);

        var endPos = startPos + directionVector * 20.0f;
        var endMapCoords = new MapCoordinates(endPos, mapId);
        EntityManager.SpawnEntity(cannon.BluespaceFlashProto, endMapCoords);
    }
}
