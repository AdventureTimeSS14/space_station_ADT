using System.Linq;
using Content.Shared.Vehicle.Components; // ADT-Tweak
using System.Numerics;
using Content.Client.Animations;
using Content.Client.Clickable;
using Content.Client.DisplacementMap;
using Content.Client.Items;
using Content.Client.Weapons.Ranged.Components;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Effects;
using Content.Shared.ADT.Hallucinations.Components;
using Content.Shared.ADT.Utility;
using Content.Shared.ADT.Weapons.Hitscan.Events;
using Content.Shared.Camera;
using Content.Shared.CCVar;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared._RMC14.Weapons.Ranged.Flamer; // ADT-Tweak
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.Animations;
using Robust.Client.ComponentTrees;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Animations;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Graphics;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SharedGunSystem = Content.Shared.Weapons.Ranged.Systems.SharedGunSystem;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem : SharedGunSystem
{
    private const float MechMuzzleOffset = 1f; // ADT-Tweak

    [Dependency] private IEyeManager _eyeManager = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private InputSystem _inputSystem = default!;
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private SharedRMCFlamerSystem _rmcFlamer = default!; // ADT-Tweak
    [Dependency] private ClickableSystem _clickable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private SpriteTreeSystem _spriteTree = default!;
    [Dependency] private DisplacementMapSystem _displacement = default!;

    public static readonly EntProtoId HitscanProto = "HitscanEffect";
    public const string ImpactProto = "ImpactEffect";
    private DisplacementEffect _displacementEffect = null!;
    private GunTargetEntityComparer _comparer = default!;

    public bool SpreadOverlay
    {
        get => _spreadOverlay;
        set
        {
            if (_spreadOverlay == value)
                return;

            _spreadOverlay = value;

            if (_spreadOverlay)
            {
                _overlayManager.AddOverlay(new GunSpreadOverlay(
                    EntityManager,
                    _eyeManager,
                    Timing,
                    _inputManager,
                    _player,
                    this,
                    TransformSystem));
            }
            else
            {
                _overlayManager.RemoveOverlay<GunSpreadOverlay>();
            }
        }
    }

    private bool _spreadOverlay;
    private bool _tracesEnabled = true;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;
        SubscribeLocalEvent<AmmoCounterComponent, ItemStatusCollectMessage>(OnAmmoCounterCollect);
        SubscribeAllEvent<MuzzleFlashEvent>(OnMuzzleFlash);

        // Plays animated effects on the client.
        SubscribeNetworkEvent<HitscanEvent>(OnHitscan);
        _cfg.OnValueChanged(ADTCCVars.TracesEnabled, OnTracesEnabledChanged, true);
        _displacementEffect = _proto.Index<DisplacementEffect>("displacementEffect");

        InitializeMagazineVisuals();
// ADT-Tweak Start
//         InitializeSpentAmmo();
// ADT-Tweak End
        InitializePrediction();

        _comparer = new GunTargetEntityComparer();
    }

    public override void Shutdown()
    {
        _cfg.UnsubValueChanged(ADTCCVars.TracesEnabled, OnTracesEnabledChanged);
        base.Shutdown();
    }

    private void OnTracesEnabledChanged(bool enabled) => _tracesEnabled = enabled;


    private void OnMuzzleFlash(MuzzleFlashEvent args)
    {
        var gunUid = GetEntity(args.Uid);

        CreateEffect(gunUid, args, gunUid);
    }

    private void OnHitscan(HitscanEvent ev)
    {
// ADT-Tweak Start
//         // ALL I WANT IS AN ANIMATED EFFECT
//
//         // TODO EFFECTS
//         // This is very jank
//         // because the effect consists of three unrelatd entities, the hitscan beam can be split appart.
//         // E.g., if a grid rotates while part of the beam is parented to the grid, and part of it is parented to the map.
//         // Ideally, there should only be one entity, with one sprite that has multiple layers
//         // Or at the very least, have the other entities parented to the same entity to make sure they stick together.
// ADT-Tweak End
        // Starlight Shooting 2.0 multi-segment traces
        if (ev.Traces is { Count: > 0 } traces)
        {
            var skip = TryConsumePredictedHitscan(ev);
            var delay = 0f;
            for (var i = skip; i < traces.Count; i++)
                delay = FireTraceEffect(ev, delay, traces[i]);
            return;
        }

        // Legacy sprite list (BSA / lasers)
        if (ev.Sprites is null || !_tracesEnabled)
            return;

        foreach (var a in ev.Sprites)
        {
            if (a.Sprite is not SpriteSpecifier.Rsi rsi)
                continue;

            var coords = GetCoordinates(a.coordinates);

            if (!TryComp(coords.EntityId, out TransformComponent? relativeXform))
                continue;

            var ent = Spawn(HitscanProto, coords);
            var sprite = Comp<SpriteComponent>(ent);

            var xform = Transform(ent);
            var targetWorldRot = a.angle + _xform.GetWorldRotation(relativeXform);
            var delta = targetWorldRot - _xform.GetWorldRotation(xform);
            _xform.SetLocalRotationNoLerp(ent, xform.LocalRotation + delta, xform);

            sprite[EffectLayers.Unshaded].AutoAnimated = false;
            _sprite.LayerSetSprite((ent, sprite), EffectLayers.Unshaded, rsi);
            _sprite.LayerSetRsiState((ent, sprite), EffectLayers.Unshaded, rsi.RsiState);
            _sprite.SetScale((ent, sprite), new Vector2(a.Distance, 1f));
            sprite[EffectLayers.Unshaded].Visible = true;

// ADT-Tweak Start
//             var lifetime = ev.Lifetime > 0f ? ev.Lifetime : 0.48f; // ADT-Tweak BSA
// ADT-Tweak End
            var lifetime = ev.Lifetime > 0f ? ev.Lifetime : 0.48f;

            var anim = new Animation()
            {
// ADT-Tweak Start
//                 Length = TimeSpan.FromSeconds(lifetime), // ADT-Tweak BSA
// ADT-Tweak End
                Length = TimeSpan.FromSeconds(lifetime),
                AnimationTracks =
                {
                    new AnimationTrackSpriteFlick()
                    {
                        LayerKey = EffectLayers.Unshaded,
                        KeyFrames =
                        {
                            new AnimationTrackSpriteFlick.KeyFrame(rsi.RsiState, 0f),
                        }
                    }
                }
            };

            _animPlayer.Play(ent, anim, "hitscan-effect");
        }
    }

// ADT-Tweak Start
//     public override void Update(float frameTime)
// ADT-Tweak End
    /// <summary>
    /// Starlight FireEffect — beam + impact as one visual beat.
    /// Impact spawns when the stretch ends; first impact_laser frame is a line tip that
    /// matches the beam (same angle, same Y scale), so the shot reads as one piece.
    /// </summary>
    private float FireTraceEffect(HitscanEvent visuals, float delay, HitscanTrace trace)
    {
// ADT-Tweak Start
//         base.Update(frameTime);
// ADT-Tweak End
        // The real bullet speed is so high that the bullet isn't visible at all. So, let's slow it down 5x.
        var length = trace.Distance / (visuals.Speed / 5000f);
        if (trace.MuzzleCoordinates is { } muzzleCoordinates)
        {
            if (visuals.MuzzleFlash is { } muzzle && (_tracesEnabled || visuals.Bullet is null))
                RenderFlash(muzzleCoordinates, trace.Angle, muzzle, 1f, false, false, length, delay, visuals);

// ADT-Tweak Start
//         if (!Timing.IsFirstTimePredicted)
// ADT-Tweak End
            if (visuals.Bullet is { } bullet)
                RenderBullet(muzzleCoordinates, trace.Angle, bullet, trace.Distance, length, delay);
        }

        if (visuals.TravelFlash is { } travel && trace.TravelCoordinates is { } travelCoordinates && (_tracesEnabled || visuals.Bullet is null))
            RenderFlash(travelCoordinates, trace.Angle, travel, trace.Distance, true, false, length, delay, visuals);

        delay += length;

        if ((visuals.ImpactFlash is not null || trace.ImpactedEnt is not null) && (_tracesEnabled || visuals.Bullet is null))
        {
            // Starlight waits for the beam, then spawns impact. Pass delay=0 here (not delay
            // again) so the tip flash starts at arrival — their upstream double-delay made the
            // burst nearly invisible, but the tip still looked "solid" via LayerSetRsiState.
            var arrival = delay;
            Timer.Spawn((int) arrival, () =>
            {
                if (visuals.ImpactFlash is { } impact)
                    RenderFlash(trace.ImpactCoordinates, trace.Angle, impact, 1f, false, true, length, 0f, visuals);

                if (visuals.ImpactSparks is { } sparks)
                    RenderImpactSparks(trace.ImpactCoordinates, trace.Angle, sparks);

                if (trace.ImpactedEnt is { } netEnt && GetEntity(netEnt) is { Valid: true } ent)
                    RenderDisplacementImpact(GetCoordinates(trace.ImpactCoordinates), trace.Angle, ent, visuals);
            });
        }

        return delay;
    }

    private void RenderDisplacementImpact(EntityCoordinates coords, Angle angle, EntityUid target, HitscanEvent visuals)
    {
        if (!TryComp(coords.EntityId, out TransformComponent? relativeXform))
            return;

        // Walls and floors keep a hole. Mobs get the wound and blood on the tile.
        if (!HasComp<MobStateComponent>(target) && visuals.BulletHole is { } hole)
            SpawnBulletHole(coords, angle, relativeXform, hole, visuals.BulletHoleOffset);

        if (!TryComp<SpriteComponent>(target, out var sprite))
            return;

        // Displacement map is authored for 32×32; pick the first matching layer (not merely the first RSI layer).
        if (!sprite.AllLayers.TryFirstOrDefault(
                x => (x.ActualRsi ?? x.Rsi) != null
                     && x.RsiState.IsValid
                     && x.PixelSize.X == 32
                     && x.PixelSize.Y == 32,
                out var layer))
            return;

        var ent = Spawn(ImpactProto, coords);
        var spriteComp = Comp<SpriteComponent>(ent);
        var spriteEnt = (ent, spriteComp);

        var xform = Transform(ent);
        var targetWorldRot = angle + _xform.GetWorldRotation(relativeXform);
        var delta = targetWorldRot - _xform.GetWorldRotation(xform);
        _xform.SetLocalRotationNoLerp(ent, xform.LocalRotation + delta, xform);

        // Directional layers counter-rotate, so the copied side stays upright and faces
        // along the shot, into the target. The displacement streak rotates with the entity.

        // Layer map key must be the string "unshaded" (see ImpactEffect prototype) so
        // DisplacementMapSystem can resolve CopyToShaderParameters.LayerKey.
        const string impactLayer = "unshaded";
        _sprite.LayerSetRsi(spriteEnt, impactLayer, (layer.ActualRsi ?? layer.Rsi)!);
        _sprite.LayerSetRsiState(spriteEnt, impactLayer, layer.RsiState);
        spriteComp[impactLayer].Visible = true;
        _displacement.TryAddDisplacement(_displacementEffect.Displacement, spriteEnt, 0, impactLayer, out _);
    }

    private void RenderBullet(NetCoordinates coordinates, Angle angle, ExtendedSpriteSpecifier sprite, float distance, float length, float delay)
    {
        if (sprite.Sprite is not SpriteSpecifier.Rsi rsi)
            return;

        var coords = GetCoordinates(coordinates);
        if (!TryComp(coords.EntityId, out TransformComponent? relativeXform))
            return;

        var ent = Spawn(HitscanProto, coords);
        RecordPredictedEffect(ent); // Starlight
        var spriteComp = Comp<SpriteComponent>(ent);
        var spriteEnt = (ent, spriteComp);

        var xform = Transform(ent);
        var targetWorldRot = angle + _xform.GetWorldRotation(relativeXform);
        var delta = targetWorldRot - _xform.GetWorldRotation(xform);
        _xform.SetLocalRotationNoLerp(ent, xform.LocalRotation + delta, xform);

        spriteComp[EffectLayers.Unshaded].AutoAnimated = false;
        spriteComp[EffectLayers.Unshaded].Visible = true;
        _sprite.LayerSetSprite(spriteEnt, EffectLayers.Unshaded, rsi);
        _sprite.LayerSetRsiState(spriteEnt, EffectLayers.Unshaded, rsi.RsiState);
        _sprite.SetOffset(spriteEnt, new Vector2(1f, 0f));
        _sprite.SetRotation(spriteEnt, 1.5708f);
        _sprite.SetColor(spriteEnt, sprite.SpriteColor);
        _sprite.SetVisible(spriteEnt, delay == 0);

        var time = delay + length;
        var despawn = Comp<TimedDespawnComponent>(ent);
        // Lifetime in seconds; EffectVisuals also deletes on anim end. Do not add +1000s.
        despawn.Lifetime = time / 1000f + 0.15f;

        if (delay != 0)
            Timer.Spawn((int) delay, () =>
            {
                if (TryComp(ent, out spriteComp))
                    _sprite.SetVisible((ent, spriteComp), true);
            });

        Timer.Spawn((int) time, () =>
        {
            if (TryComp(ent, out spriteComp))
                _sprite.SetVisible((ent, spriteComp), false);
        });

        var anim = new Animation()
        {
            Length = TimeSpan.FromMilliseconds(time),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty()
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(new Vector2(1f, 0f), delay / 1000f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(distance + 1.0f, 0f), length / 1000f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(distance + 1.0f, 0f), 0.15f),
                    },
                    InterpolationMode = AnimationInterpolationMode.Linear
                }
            }
        };

        _animPlayer.Play(ent, anim, "hitscan-effect");
    }

    /// <summary>
    /// Starlight-style flash with ADT thickness. Impact linger is longer than stock +100ms:
    /// impact_laser needs ~0.6s to reach the burst frames; 100ms left it on the thin tip
    /// frame so the hit looked random / missing. EffectVisuals deletes when this anim ends.
    /// </summary>
    private void RenderFlash(NetCoordinates coordinates, Angle angle, SpriteSpecifier sprite, float distance, bool travel, bool end, float length, float delay, HitscanEvent visuals)
    {
        if (end)
            length = 0;

        // Linger comes from the hitscan prototype. Firearm fades are about 0.5–0.7s;
        // impact_laser starts its burst at 0.8s and holds the last frame for 0.5s.
        var lingerMs = (end ? visuals.ImpactLinger : travel ? visuals.TravelLinger : visuals.MuzzleLinger) * 1000f;
        var time = delay + length + lingerMs;

        if (sprite is not SpriteSpecifier.Rsi rsi)
            return;

        var coords = GetCoordinates(coordinates);
        if (!TryComp(coords.EntityId, out TransformComponent? relativeXform))
            return;

// ADT-Tweak Start
//         var entityNull = _player.LocalEntity;
// ADT-Tweak End
        var ent = Spawn(HitscanProto, coords);
        var spriteComp = Comp<SpriteComponent>(ent);
        var spriteEnt = (ent, spriteComp);

        var xform = Transform(ent);
        var targetWorldRot = angle + _xform.GetWorldRotation(relativeXform);
        var delta = targetWorldRot - _xform.GetWorldRotation(xform);
        _xform.SetLocalRotationNoLerp(ent, xform.LocalRotation + delta, xform);

        _sprite.LayerSetSprite(spriteEnt, EffectLayers.Unshaded, rsi);
        _sprite.LayerSetRsiState(spriteEnt, EffectLayers.Unshaded, rsi.RsiState);
        // Hold the first frame while the shot travels, then play the fade through to the last frame.
        // A manual flick kept rewriting the same timestamp and froze the animation in the middle.
        if (spriteComp[EffectLayers.Unshaded] is SpriteComponent.Layer flashLayer)
        {
            flashLayer.Loop = false;
            flashLayer.AutoAnimated = false;
            var fadeAt = (int) (delay + length);
            if (fadeAt <= 0)
                flashLayer.AutoAnimated = true;
            else
            {
                Timer.Spawn(fadeAt, () =>
                {
                    if (Deleted(ent) || Comp<SpriteComponent>(ent)[EffectLayers.Unshaded] is not SpriteComponent.Layer layer)
                        return;

                    layer.Loop = false;
                    layer.AutoAnimated = true;
                });
            }
        }

        var flashThickness = visuals.FlashThickness;
        var beamStart = visuals.BeamStartScale;
        // The travel entity sits on the midpoint. Keep the tail on the muzzle while the head
        // reaches the impact exactly when the impact flash is spawned.
        var beamStartOffset = travel ? -distance / 2f + beamStart / 2f : 0f;
        if (travel)
        {
            _sprite.SetScale(spriteEnt, new Vector2(beamStart, flashThickness));
            _sprite.SetOffset(spriteEnt, new Vector2(beamStartOffset, 0f));
        }
        else
            _sprite.SetScale(spriteEnt, new Vector2(1f, flashThickness));

        spriteComp[EffectLayers.Unshaded].Visible = true;
        _sprite.SetVisible(spriteEnt, delay == 0);

        var despawn = Comp<TimedDespawnComponent>(ent);
        despawn.Lifetime = time / 1000f + 0.15f;

        if (delay != 0)
            Timer.Spawn((int) delay, () =>
            {
                if (!Deleted(ent))
                    _sprite.SetVisible(spriteEnt, true);
            });

        Timer.Spawn((int) time, () =>
        {
            if (!Deleted(ent))
                _sprite.SetVisible(spriteEnt, false);
        });

        var anim = new Animation()
        {
            Length = TimeSpan.FromMilliseconds(time),
            AnimationTracks = { }
        };

// ADT-Tweak Start
//         if (entityNull == null || !TryComp<CombatModeComponent>(entityNull, out var combat) || !combat.IsInCombatMode)
// ADT-Tweak End
        if (travel)
        {
            // KeyTime is a gap after the previous frame, not a timestamp.
            // Stretch lasts exactly the flight time, then the short linger. Impact is spawned at the same moment.
            var stretch = length / 1000f;
            var linger = lingerMs / 1000f;
            anim.AnimationTracks.Add(new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Scale),
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(new Vector2(beamStart, flashThickness), delay / 1000f),
                    new AnimationTrackProperty.KeyFrame(new Vector2(distance, flashThickness), stretch),
                    new AnimationTrackProperty.KeyFrame(new Vector2(distance, flashThickness), linger),
                },
                InterpolationMode = AnimationInterpolationMode.Linear
            });
            anim.AnimationTracks.Add(new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Offset),
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(new Vector2(beamStartOffset, 0f), delay / 1000f),
                    new AnimationTrackProperty.KeyFrame(Vector2.Zero, stretch),
                    new AnimationTrackProperty.KeyFrame(Vector2.Zero, linger),
                },
                InterpolationMode = AnimationInterpolationMode.Linear
            });
        }

        _animPlayer.Play(ent, anim, "hitscan-effect");
    }

    private void RenderImpactSparks(NetCoordinates coordinates, Angle angle, string prototype)
    {
        var coords = GetCoordinates(coordinates);
        if (!coords.IsValid(EntityManager) || !TryComp(coords.EntityId, out TransformComponent? relativeXform))
            return;

        var ent = Spawn(prototype, coords);
        RecordPredictedEffect(ent);
        FaceAlong(ent, angle, relativeXform);
    }

    private void SpawnBulletHole(EntityCoordinates coords, Angle angle, TransformComponent relativeXform, string prototype, float offset)
    {
        var hole = Spawn(prototype, coords.Offset(angle.ToVec() * offset));
        RecordPredictedEffect(hole);
        FaceAlong(hole, angle, relativeXform);
    }

    private void FaceAlong(EntityUid ent, Angle angle, TransformComponent relativeXform)
    {
        var xform = Transform(ent);
        var targetWorldRot = angle + _xform.GetWorldRotation(relativeXform);
        var delta = targetWorldRot - _xform.GetWorldRotation(xform);
        _xform.SetLocalRotationNoLerp(ent, xform.LocalRotation + delta, xform);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_player.LocalEntity is not EntityUid entity)
            return;

        foreach (var heldGun in EnumerateGuns(entity))
        {
            if (heldGun.Comp.NextFire <= Timing.CurTime && heldGun.Comp.BurstActivated)
                ContinueBurst(entity, heldGun);
        }

// ADT-Tweak Start
//         var entity = entityNull.Value;
// ADT-Tweak End
        if (!Timing.IsFirstTimePredicted)
            return;

        if (!TryComp<CombatModeComponent>(entity, out var combat) || !combat.IsInCombatMode)
            return;

        if (!TryGetGun(entity, out var gun))
        {
            return;
        }

        var useKey = gun.Comp.UseKey ? EngineKeyFunctions.Use : EngineKeyFunctions.UseSecondary;

// ADT-Tweak Start
//         if (_inputSystem.CmdStates.GetState(useKey) != BoundKeyState.Down && !gun.Comp.BurstActivated)
// ADT-Tweak End
        if (_inputSystem.CmdStates.GetState(useKey) != BoundKeyState.Down) // Starlight-edit
        {
            if (gun.Comp.ShotCounter != 0)
                RaisePredictiveEvent(new RequestStopShootEvent { Gun = GetNetEntity(gun) });
            return;
        }

        if (gun.Comp.NextFire > Timing.CurTime)
            return;

        var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);

        if (mousePos.MapId == MapId.Nullspace)
        {
            if (gun.Comp.ShotCounter != 0)
                RaisePredictiveEvent(new RequestStopShootEvent { Gun = GetNetEntity(gun) });

            return;
        }

        // Define target coordinates relative to gun entity, so that network latency on moving grids doesn't fuck up the target location.
        var target = GetBestTarget(_eyeManager.CurrentEye, mousePos);

        var coordinates = TransformSystem.ToCoordinates(entity, mousePos);

        Log.Debug($"Sending shoot request tick {Timing.CurTick} / {Timing.CurTime}");

        RaisePredictiveEvent(new RequestShootEvent
        {
            Target = target,
            Coordinates = GetNetCoordinates(coordinates),
            Gun = GetNetEntity(gun),
            Continuous = _cfg.GetCVar(CCVars.ControlHoldToAttackRanged),
            Tick = Timing.CurTick, // Starlight
        });
    }

    public override void Shoot(Entity<GunComponent> gun, List<(EntityUid? Entity, IShootable Shootable)> ammo,
        EntityCoordinates fromCoordinates, EntityCoordinates toCoordinates, out bool userImpulse, EntityUid? user = null, bool throwItems = false)
    {
        userImpulse = true;

        // Rather than splitting client / server for every ammo provider it's easier
        // to just delete the spawned entities. This is for programmer sanity despite the wasted perf.
        // This also means any ammo specific stuff can be grabbed as necessary.
// ADT-Tweak Start
//         var direction = TransformSystem.ToMapCoordinates(fromCoordinates).Position - TransformSystem.ToMapCoordinates(toCoordinates).Position;
//         var worldAngle = direction.ToAngle().Opposite();
// ADT-Tweak End
        var fromMap = TransformSystem.ToMapCoordinates(fromCoordinates);
        var toMap = TransformSystem.ToMapCoordinates(toCoordinates).Position;
        var mapDirection = GetShotMapDirection(gun, fromMap.Position, toMap);
        var direction = -mapDirection;
        var worldAngle = mapDirection.ToAngle();
        var ammoIndex = -1;

        foreach (var (ent, shootable) in ammo)
        {
            ammoIndex++;
            if (throwItems)
            {
                Recoil(user, direction, gun.Comp.CameraRecoilScalarModified);
                if (IsClientSide(ent!.Value))
                    Del(ent.Value);
                else
                    RemoveShootable(ent.Value);
                continue;
            }

            // TODO: Clean this up in a gun refactor at some point - too much copy pasting
            switch (shootable)
            {
                case CartridgeAmmoComponent cartridge:
                    if (!cartridge.Spent)
                    {
                        SetCartridgeSpent(ent!.Value, cartridge, true);
                        PredictCartridge(gun, cartridge.Prototype, ammoIndex, fromMap, mapDirection, user);
                        MuzzleFlash(gun, cartridge, worldAngle, user);
                        if (TryComp<MechComponent>(user, out var cmech))    // ADT Mechs
                        {
                            Audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, CompOrNull<VehicleComponent>(user)?.Operator);
                            Recoil(CompOrNull<VehicleComponent>(user)?.Operator, direction, gun.Comp.CameraRecoilScalarModified);
                        }
                        else
                        {
                            Audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, user);
                            Recoil(user, direction, gun.Comp.CameraRecoilScalarModified);
                        }
                        // TODO: Can't predict entity deletions.
                        //if (cartridge.DeleteOnSpawn)
                        //    Del(cartridge.Owner);
                    }
                    else
                    {
                        userImpulse = false;
                        Audio.PlayPredicted(gun.Comp.SoundEmpty, gun, user);
                    }

                    // Starlight-start
                    if (!cartridge.DeleteOnSpawn && !Containers.IsEntityInContainer(ent!.Value) && !gun.Comp.Pump)
                        Audio.PlayPredicted(cartridge.EjectSound, gun, user, EjectSoundParams);
                    // Starlight-end

                    if (IsClientSide(ent!.Value))
                        Del(ent.Value);

                    break;
                case AmmoComponent newAmmo:
                    MuzzleFlash(gun, newAmmo, worldAngle, user);
                    if (TryComp<MechComponent>(user, out var mech)) // ADT Mechs
                    {
                        Audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, CompOrNull<VehicleComponent>(user)?.Operator);
                        Recoil(CompOrNull<VehicleComponent>(user)?.Operator, direction, gun.Comp.CameraRecoilScalarModified);
                    }
                    else
                    {
                        Audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, user);
                        Recoil(user, direction, gun.Comp.CameraRecoilScalarModified);
                    }
                    if (IsClientSide(ent!.Value))
                        Del(ent.Value);
                    else
                        RemoveShootable(ent.Value);
                    break;
                case HitscanAmmoComponent:
                    if (HasComp<MechComponent>(user)) // ADT-tweak
                    {
                        Audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, CompOrNull<VehicleComponent>(user)?.Operator);
                        Recoil(CompOrNull<VehicleComponent>(user)?.Operator, direction, gun.Comp.CameraRecoilScalarModified);
                    }
                    else
                    {
                        Audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, user);
                        Recoil(user, direction, gun.Comp.CameraRecoilScalarModified);
                    }

                    // Starlight #6369: client hitscan ammo left invisible entities on mechs.
                    if (ent != null && IsClientSide(ent.Value))
                        Del(ent.Value);
                    break;
                // ADT-Tweak-start
                case RMCFlamerAmmoProviderComponent flamer:
                    if (ent == null)
                        break;

                    _rmcFlamer.ShootFlamer((ent.Value, flamer), gun, user, fromCoordinates, toCoordinates);
                    break;
                // ADT-Tweak-end
            }
        }
    }

    private void Recoil(EntityUid? user, Vector2 recoil, float recoilScalar)
    {
        if (!Timing.IsFirstTimePredicted || user == null || recoil == Vector2.Zero || recoilScalar == 0)
            return;

// ADT-Tweak Start
//         _recoil.KickCamera(user.Value, recoil.Normalized() * 0.5f * recoilScalar);
// ADT-Tweak End
        var shakeIntensity = _cfg.GetCVar(CCVars.ScreenShakeIntensity);
        _recoil.KickCamera(user.Value, recoil.Normalized() * (0.5f + ((0.13f - 0.5f) * shakeIntensity)) * recoilScalar); // ADT screenshake
    }

    protected override void CreateEffect(EntityUid gunUid, MuzzleFlashEvent message, EntityUid? tracked = null)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        // EntityUid check added to stop throwing exceptions due to https://github.com/space-wizards/space-station-14/issues/28252
        // TODO: Check to see why invalid entities are firing effects.
        if (gunUid == EntityUid.Invalid)
        {
            Log.Debug($"Invalid Entity sent MuzzleFlashEvent (proto: {message.Prototype}, gun: {ToPrettyString(gunUid)})");
            return;
        }

        var gunXform = Transform(gunUid);
        var gridUid = gunXform.GridUid;
        EntityCoordinates coordinates;

        if (TryComp(gridUid, out MapGridComponent? mapGrid))
        {
            coordinates = new EntityCoordinates(gridUid.Value, _maps.LocalToGrid(gridUid.Value, mapGrid, gunXform.Coordinates));
        }
        else if (gunXform.MapUid != null)
        {
            coordinates = new EntityCoordinates(gunXform.MapUid.Value, TransformSystem.GetWorldPosition(gunXform));
        }
        else
        {
            return;
        }

        // ADT-Tweak-start
        var trackOffset = Vector2.UnitX / 2f;
        var mechOwner = gunXform.ParentUid;
        if (HasComp<MechEquipmentComponent>(gunUid) && HasComp<MechComponent>(mechOwner))
        {
            tracked = mechOwner;
            trackOffset = Vector2.UnitX * MechMuzzleOffset;
        }

        if (HasComp<HallucinationsRemoveMobsComponent>(_player.LocalEntity) && tracked != _player.LocalEntity && !HasComp<HallucinationComponent>(tracked))
            tracked = null;
        // ADT-Tweak-end

        var ent = Spawn(message.Prototype, coordinates);
        TransformSystem.SetWorldRotationNoLerp(ent, message.Angle);

        if (tracked != null)
        {
            var track = EnsureComp<TrackUserComponent>(ent);
            track.User = tracked;
            track.Offset = trackOffset; // ADT-Tweak
        }

        var lifetime = 0.4f;

        if (TryComp<TimedDespawnComponent>(gunUid, out var despawn))
        {
            lifetime = despawn.Lifetime;
        }

        var anim = new Animation()
        {
            Length = TimeSpan.FromSeconds(lifetime),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Color.White.WithAlpha(1f), 0),
                        new AnimationTrackProperty.KeyFrame(Color.White.WithAlpha(0f), lifetime)
                    }
                }
            }
        };

        _animPlayer.Play(ent, anim, "muzzle-flash");
        if (!TryComp(gunUid, out PointLightComponent? light))
        {
            light = Factory.GetComponent<PointLightComponent>();
            light.NetSyncEnabled = false;
            AddComp(gunUid, light);
        }

        Lights.SetEnabled(gunUid, true, light);
        Lights.SetRadius(gunUid, 2f, light);
        Lights.SetColor(gunUid, Color.FromHex("#cc8e2b"), light);
        Lights.SetEnergy(gunUid, 5f, light);

        var animTwo = new Animation()
        {
            Length = TimeSpan.FromSeconds(lifetime),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(PointLightComponent),
                    Property = nameof(PointLightComponent.Energy),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(5f, 0),
                        new AnimationTrackProperty.KeyFrame(0f, lifetime)
                    }
                },
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(PointLightComponent),
                    Property = nameof(PointLightComponent.AnimatedEnable),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(true, 0),
                        new AnimationTrackProperty.KeyFrame(false, lifetime)
                    }
                }
            }
        };

        var uidPlayer = EnsureComp<AnimationPlayerComponent>(gunUid);

        _animPlayer.Stop(gunUid, uidPlayer, "muzzle-flash-light");
        _animPlayer.Play((gunUid, uidPlayer), animTwo, "muzzle-flash-light");
    }

    /// <remarks>We use our own sorting algorithm separate from the default for smarter configurability.</remarks>
    private NetEntity? GetBestTarget(IEye eye, MapCoordinates coordinates)
    {
        // Find all the entities intersecting our click
        var entities = _spriteTree.QueryAabb(coordinates.MapId, Box2.CenteredAround(coordinates.Position, new Vector2(1, 1)));

        // Check the entities against whether or not we can click them
        var foundEntities = new List<(EntityUid, bool, bool, int, uint, float, float)>(entities.Count);

        foreach (var entity in entities)
        {
            // Don't add the target if we can't shoot the target!
            if (!CheckFixtures(entity.Uid))
                continue;

            var entry = CheckTarget((entity.Uid, entity.Component, entity.Transform), eye, coordinates);
            foundEntities.Add(entry);
        }

        if (foundEntities.Count == 0)
            return null;

        // Do drawdepth & y-sorting. First index is the top-most sprite (opposite of normal render order).
        foundEntities.Sort(_comparer);
        var (target, alive, occluded, _, _, _, _) = foundEntities.FirstOrDefault();

        // Prevents us from just selecting a random target nearby our cursor. It must either be alive, or our cursor must be on top of it!
        if (!occluded && !alive)
            return null;

        return GetNetEntity(target);
    }

    private (EntityUid, bool, bool, int, uint, float, float) CheckTarget(Entity<SpriteComponent, TransformComponent> target, IEye eye, MapCoordinates coordinates)
    {
        var occluded = _clickable.CheckClick((target.Owner, null, target.Comp1, target.Comp2),
            coordinates.Position,
            eye,
            true,
            out var drawDepthClicked,
            out var renderOrder,
            out var bottom);

        var difference = (target.Comp2.Coordinates.Position - coordinates.Position).LengthSquared();

        return (target.Owner, _mobState.IsAlive(target.Owner), occluded, drawDepthClicked, renderOrder, bottom, difference);
    }

    /// <summary>
    /// This Comparer takes a list of Entities that we can hit and orders them by which target the player is probably trying to shoot.
    /// We organize based on these criteria in this order:
    /// alive means the entity has a MobState and is currently alive. We check it first since they typically shoot back.
    /// occluded is whether the cursor is above the sprite or just near it.
    /// depth is the order in which sprites are layered, bigger number means its rendered above others.
    /// renderOrder is used to indicate if a sprite should be visually more important, typically this value is 0.
    /// bottom indicates which sprite is visually the lowest on the screen and therefore typically above other sprites.
    /// distance indicates the distance from the entity's coordinates to our mouse.
    /// If all of those tie, then we organize by whichever entity has the highest EntityUid.
    /// </summary>
    private sealed class GunTargetEntityComparer : IComparer<(EntityUid clicked, bool alive, bool occluded, int depth, uint renderOrder, float bottom, float distance)>
    {
        public int Compare((EntityUid clicked, bool alive, bool occluded, int depth, uint renderOrder, float bottom, float distance) x,
            (EntityUid clicked, bool alive, bool occluded, int depth, uint renderOrder, float bottom, float distance) y)
        {
            var cmp = y.occluded.CompareTo(x.occluded);

            if (cmp != 0)
            {
                return cmp;
            }

            cmp = y.alive.CompareTo(x.alive);
            if (cmp != 0)
            {
                return cmp;
            }

            cmp = y.depth.CompareTo(x.depth);
            if (cmp != 0)
            {
                return cmp;
            }

            cmp = y.renderOrder.CompareTo(x.renderOrder);

            if (cmp != 0)
            {
                return cmp;
            }

            cmp = -y.bottom.CompareTo(x.bottom);

            if (cmp != 0)
            {
                return cmp;
            }

            cmp = -y.distance.CompareTo(x.distance);

            if (cmp != 0)
            {
                return cmp;
            }

            return y.clicked.CompareTo(x.clicked);
        }
    }

    private bool CheckFixtures(Entity<FixturesComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return false;

        // TODO: Maybe also check that our cursor is intersecting a valid fixture?
        foreach (var fix in entity.Comp.Fixtures)
        {
            if (!fix.Value.Hard || (fix.Value.CollisionLayer & (int)CollisionGroup.BulletImpassable) == 0)
                continue;

            // Only need to check if we're hitting one fixture
            return true;
        }

        // If we cannot collide then we absolutely do not want to target it!
        return false;
    }

    public override void PlayImpactSound(EntityUid otherEntity, DamageSpecifier? modifiedDamage, SoundSpecifier? weaponSound, bool forceWeaponSound) { }
}
