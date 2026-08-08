using System.Linq;
using System.Numerics;
using Content.Client.Animations;
using Content.Client.DisplacementMap;
using Content.Client.Gameplay;
using Content.Client.Items;
using Content.Client.Weapons.Ranged.Components;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Effects;
using Content.Shared.ADT.Utility;
using Content.Shared.ADT.Weapons.Hitscan.Events;
using Content.Shared.Camera;
using Content.Shared.CCVar;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Mech.Components;
using Content.Shared.Weapons.Hitscan.Components;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Animations;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using SharedGunSystem = Content.Shared.Weapons.Ranged.Systems.SharedGunSystem;
using TimedDespawnComponent = Robust.Shared.Spawners.TimedDespawnComponent;
using Direction = Robust.Shared.Maths.Direction;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem : SharedGunSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly InputSystem _inputSystem = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IStateManager _state = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly DisplacementMapSystem _displacement = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public static readonly EntProtoId HitscanProto = "HitscanEffect";
    public const string ImpactProto = "ImpactEffect";
    private DisplacementEffect _displacementEffect = null!;

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
        InitializeSpentAmmo();
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
        // Starlight Shooting 2.0 multi-segment traces
        if (ev.Traces is { Count: > 0 })
        {
            var delay = 0f;
            foreach (var trace in ev.Traces)
                delay = FireTraceEffect(ev, delay, trace);
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

            var lifetime = ev.Lifetime > 0f ? ev.Lifetime : 0.48f;

            var anim = new Animation()
            {
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

    /// <summary>
    /// Starlight FireEffect — beam + impact as one visual beat.
    /// Impact spawns when the stretch ends; first impact_laser frame is a line tip that
    /// matches the beam (same angle, same Y scale), so the shot reads as one piece.
    /// </summary>
    private float FireTraceEffect(HitscanEvent visuals, float delay, HitscanTrace trace)
    {
        // The real bullet speed is so high that the bullet isn't visible at all. So, let's slow it down 5x.
        var length = trace.Distance / (visuals.Speed / 5000f);
        if (trace.MuzzleCoordinates is { } muzzleCoordinates)
        {
            if (visuals.MuzzleFlash is { } muzzle && (_tracesEnabled || visuals.Bullet is null))
                RenderFlash(muzzleCoordinates, trace.Angle, muzzle, 1f, false, false, length, delay);

            if (visuals.Bullet is { } bullet)
                RenderBullet(muzzleCoordinates, trace.Angle, bullet, trace.Distance - 1.5f, length, delay);
        }

        if (visuals.TravelFlash is { } travel && trace.TravelCoordinates is { } travelCoordinates && (_tracesEnabled || visuals.Bullet is null))
            RenderFlash(travelCoordinates, trace.Angle, travel, trace.Distance - 1.5f, true, false, length, delay);

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
                    RenderFlash(trace.ImpactCoordinates, trace.Angle, impact, 1f, false, true, length, 0f);

                if (trace.ImpactedEnt is { } netEnt && GetEntity(netEnt) is { Valid: true } ent)
                    RenderDisplacementImpact(GetCoordinates(trace.ImpactCoordinates), trace.Angle, ent);
            });
        }

        return delay;
    }

    private void RenderDisplacementImpact(EntityCoordinates coords, Angle angle, EntityUid target)
    {
        if (!TryComp<SpriteComponent>(target, out var sprite))
            return;

        if (!TryComp(coords.EntityId, out TransformComponent? relativeXform))
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

        // Lock RSI cardinals. Mob layers are 4/8-dir; if we also rotate the entity by shot
        // angle, EffectiveDirection picks a different facing each time — the wound then
        // appears to face toward the character or away at random. South + entity rotation
        // keeps bullet_impact.rsi's rightward bias aligned with the shot.
        spriteComp.EnableDirectionOverride = true;
        spriteComp.DirectionOverride = Direction.South;

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
                        new AnimationTrackProperty.KeyFrame(new Vector2(distance + 1.0f, 0f), time / 1000f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(distance + 1.0f, 0f), (time + 1000f) / 1000f),
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
    private void RenderFlash(NetCoordinates coordinates, Angle angle, SpriteSpecifier sprite, float distance, bool travel, bool end, float length, float delay)
    {
        if (end)
            length = 0;

        // Beam: Starlight +100ms. Impact: enough for burst frames without keeping sprites too long.
        var lingerMs = end ? 400f : 100f;
        var time = delay + length + lingerMs;

        if (sprite is not SpriteSpecifier.Rsi rsi)
            return;

        var coords = GetCoordinates(coordinates);
        if (!TryComp(coords.EntityId, out TransformComponent? relativeXform))
            return;

        var ent = Spawn(HitscanProto, coords);
        var spriteComp = Comp<SpriteComponent>(ent);
        var spriteEnt = (ent, spriteComp);

        var xform = Transform(ent);
        var targetWorldRot = angle + _xform.GetWorldRotation(relativeXform);
        var delta = targetWorldRot - _xform.GetWorldRotation(xform);
        _xform.SetLocalRotationNoLerp(ent, xform.LocalRotation + delta, xform);

        spriteComp[EffectLayers.Unshaded].AutoAnimated = false;
        _sprite.LayerSetSprite(spriteEnt, EffectLayers.Unshaded, rsi);
        _sprite.LayerSetRsiState(spriteEnt, EffectLayers.Unshaded, rsi.RsiState);

        const float flashThickness = 1f;
        if (travel)
        {
            _sprite.SetScale(spriteEnt, new Vector2(0.05f, flashThickness));
            _sprite.SetOffset(spriteEnt, new Vector2(distance * -0.5f, 0f));
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

        // SpriteFlick KeyTime is delta from previous keyframe. Impact: scrub from arrival (0).
        // Beam/muzzle: Starlight flick near stretch end.
        var flickAt = end ? delay / 1000f : Math.Max(0f, (time - lingerMs) / 1000f);

        var anim = new Animation()
        {
            Length = TimeSpan.FromMilliseconds(time),
            AnimationTracks =
            {
                new AnimationTrackSpriteFlick()
                {
                    LayerKey = EffectLayers.Unshaded,
                    KeyFrames =
                    {
                        new AnimationTrackSpriteFlick.KeyFrame(rsi.RsiState, flickAt),
                    }
                }
            }
        };

        if (travel)
        {
            var stretchEnd = Math.Max(0f, (time - lingerMs) / 1000f);
            anim.AnimationTracks.Add(new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Scale),
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(new Vector2(0.05f, flashThickness), delay / 1000f),
                    new AnimationTrackProperty.KeyFrame(new Vector2(distance, flashThickness), stretchEnd),
                    new AnimationTrackProperty.KeyFrame(new Vector2(distance, flashThickness), time / 1000f),
                },
                InterpolationMode = AnimationInterpolationMode.Linear
            });
            anim.AnimationTracks.Add(new AnimationTrackComponentProperty()
            {
                ComponentType = typeof(SpriteComponent),
                Property = nameof(SpriteComponent.Offset),
                KeyFrames =
                {
                    new AnimationTrackProperty.KeyFrame(new Vector2(distance * -0.5f, 0f), delay / 1000f),
                    new AnimationTrackProperty.KeyFrame(new Vector2(0, 0f), stretchEnd),
                    new AnimationTrackProperty.KeyFrame(new Vector2(0, 0f), time / 1000f),
                },
                InterpolationMode = AnimationInterpolationMode.Linear
            });
        }

        _animPlayer.Play(ent, anim, "hitscan-effect");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!Timing.IsFirstTimePredicted)
            return;

        var entityNull = _player.LocalEntity;

        if (entityNull == null || !TryComp<CombatModeComponent>(entityNull, out var combat) || !combat.IsInCombatMode)
        {
            return;
        }

        var entity = entityNull.Value;

        if (!TryGetGun(entity, out var gun))
        {
            return;
        }

        var useKey = gun.Comp.UseKey ? EngineKeyFunctions.Use : EngineKeyFunctions.UseSecondary;

        if (_inputSystem.CmdStates.GetState(useKey) != BoundKeyState.Down && !gun.Comp.BurstActivated)
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
        var coordinates = TransformSystem.ToCoordinates(entity, mousePos);

        NetEntity? target = null;
        if (_state.CurrentState is GameplayStateBase screen)
            target = GetNetEntity(screen.GetClickedEntity(mousePos));

        Log.Debug($"Sending shoot request tick {Timing.CurTick} / {Timing.CurTime}");


        RaisePredictiveEvent(new RequestShootEvent
        {
            Target = target,
            Coordinates = GetNetCoordinates(coordinates),
            Gun = GetNetEntity(gun),
            Continuous = _cfg.GetCVar(CCVars.ControlHoldToAttackRanged),
        });
    }

    public override void Shoot(Entity<GunComponent> gun, List<(EntityUid? Entity, IShootable Shootable)> ammo,
        EntityCoordinates fromCoordinates, EntityCoordinates toCoordinates, out bool userImpulse, EntityUid? user = null, bool throwItems = false)
    {
        userImpulse = true;

        // Rather than splitting client / server for every ammo provider it's easier
        // to just delete the spawned entities. This is for programmer sanity despite the wasted perf.
        // This also means any ammo specific stuff can be grabbed as necessary.
        var direction = TransformSystem.ToMapCoordinates(fromCoordinates).Position - TransformSystem.ToMapCoordinates(toCoordinates).Position;
        var worldAngle = direction.ToAngle().Opposite();

        foreach (var (ent, shootable) in ammo)
        {
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
                        MuzzleFlash(gun, cartridge, worldAngle, user);
                        if (TryComp<MechComponent>(user, out var cmech))    // ADT Mechs
                        {
                            Audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, cmech.PilotSlot.ContainedEntity);
                            Recoil(cmech.PilotSlot.ContainedEntity, direction, gun.Comp.CameraRecoilScalarModified);
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

                    if (IsClientSide(ent!.Value))
                        Del(ent.Value);

                    break;
                case AmmoComponent newAmmo:
                    MuzzleFlash(gun, newAmmo, worldAngle, user);
                    if (TryComp<MechComponent>(user, out var mech)) // ADT Mechs
                    {
                        Audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, mech.PilotSlot.ContainedEntity);
                        Recoil(mech.PilotSlot.ContainedEntity, direction, gun.Comp.CameraRecoilScalarModified);
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
                    Audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, user);
                    Recoil(user, direction, gun.Comp.CameraRecoilScalarModified);
                    if (TryComp<MechComponent>(user, out var hmech)) // ADT-tweak
                    {
                        Audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, hmech.PilotSlot.ContainedEntity);
                        Recoil(hmech.PilotSlot.ContainedEntity, direction, gun.Comp.CameraRecoilScalarModified);
                    }
                    else
                    {
                        Audio.PlayPredicted(gun.Comp.SoundGunshotModified, gun, user);
                        Recoil(user, direction, gun.Comp.CameraRecoilScalarModified);
                    }
                    break;
            }
        }
    }

    private void Recoil(EntityUid? user, Vector2 recoil, float recoilScalar)
    {
        if (!Timing.IsFirstTimePredicted || user == null || recoil == Vector2.Zero || recoilScalar == 0)
            return;

        var shakeIntensity = _cfg.GetCVar(CCVars.ScreenShakeIntensity);
        _recoil.KickCamera(user.Value, recoil.Normalized() * (0.5f + ((0.13f - 0.5f) * shakeIntensity)) * recoilScalar); // ADT screenshake
    }

    protected override void Popup(string message, EntityUid? uid, EntityUid? user)
    {
        if (uid == null || user == null || !Timing.IsFirstTimePredicted)
            return;

        PopupSystem.PopupEntity(message, uid.Value, user.Value);
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

        var ent = Spawn(message.Prototype, coordinates);
        TransformSystem.SetWorldRotationNoLerp(ent, message.Angle);

        if (tracked != null)
        {
            var track = EnsureComp<TrackUserComponent>(ent);
            track.User = tracked;
            track.Offset = Vector2.UnitX / 2f;
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

    // TODO: Move RangedDamageSoundComponent to shared so this can be predicted.
    public override void PlayImpactSound(EntityUid otherEntity, DamageSpecifier? modifiedDamage, SoundSpecifier? weaponSound, bool forceWeaponSound) { }
}
