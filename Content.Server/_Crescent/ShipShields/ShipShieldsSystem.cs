using Content.Server.Power.Components;
using Content.Server._Mono.FireControl; // Forge
using Content.Shared._Crescent.ShipShields;
using Content.Shared._Mono.SpaceArtillery;
using Content.Shared.Physics;
using Content.Shared.Projectiles;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing; // LuaM
using System.Numerics;
using Content.Server._Crescent.ShipShields.Components;


namespace Content.Server._Crescent.ShipShields;

public sealed partial class ShipShieldsSystem : EntitySystem
{
    private const string ShipShieldPrototype = "ShipShield";

    //private const float DeflectionSpread = 25f;
    private const float EmitterUpdateRate = 1.5f;
    private const int ShieldChainVertices = 64; // LuaM

    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private FixtureSystem _fixtureSystem = default!;
    [Dependency] private PhysicsSystem _physicsSystem = default!;
    [Dependency] private PvsOverrideSystem _pvsSys = default!;
    [Dependency] private FireControlSystem _fireControl = default!; // Forge
    [Dependency] private IGameTiming _timing = default!; // LuaM

    private EntityQuery<ProjectileComponent> _projectileQuery;
    private EntityQuery<ShipWeaponProjectileComponent> _shipWeaponProjectileQuery;
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateShieldVisuals(frameTime); // LuaM

        var query = EntityQueryEnumerator<ShipShieldEmitterComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out var uid, out var emitter, out var power))
        {
            emitter.Accumulator += frameTime;

            if (emitter.Accumulator < EmitterUpdateRate)
                continue;

            if (CalculateLoadDamage(emitter) >= emitter.MaxDraw)
                emitter.Recharging = true;
            if (!power.Powered)
                emitter.Recharging = true;

            emitter.Accumulator -= EmitterUpdateRate;
            if (emitter.OverloadAccumulator > 0)
            {
                emitter.OverloadAccumulator -= EmitterUpdateRate;
            }

            float healed = emitter.HealPerSecond * EmitterUpdateRate;

            if (emitter.Recharging)
                healed *= emitter.UnpoweredBonus;

            emitter.Damage -= healed;

            if (emitter.Damage < 0)
            {
                emitter.Damage = 0;
                if (power.Powered)
                    emitter.Recharging = false;
            }

            AdjustEmitterLoad(uid, emitter, power);

            var parent = Transform(uid).GridUid;

            if (parent == null)
                continue;

            var filter = _station.GetInOwningStation(uid);

            if (emitter.Damage > emitter.DamageLimit)
                emitter.OverloadAccumulator = emitter.DamageOverloadTimePunishment;

            if (!emitter.Recharging && emitter.Shield is null && emitter.OverloadAccumulator < 1)
            {
                var shield = ShieldEntity(parent.Value, uid);
                if (shield != EntityUid.Invalid)
                {
                    emitter.Shield = shield;
                    emitter.Shielded = parent.Value;
                }
                _audio.PlayGlobal(emitter.PowerUpSound, filter, true, emitter.PowerUpSound.Params);
            }
            else if ((emitter.Recharging || emitter.OverloadAccumulator > 0) && emitter.Shield is not null || HasComp<ShipShieldDisabledGridComponent>(Transform(uid).GridUid))
            {
                UnshieldEntity(parent.Value);
                emitter.Shield = null;
                emitter.Shielded = null;
                if (!HasComp<ShipShieldDisabledGridComponent>(Transform(uid).GridUid))
                    _audio.PlayGlobal(emitter.PowerDownSound, filter, true, emitter.PowerDownSound.Params); // LuaM: PowerUpSound.Params > PowerDownSound.Params
            }

            // Forge-Change-Start
            // Push fresh shield state to any consoles on this grid so HP %/recharge timer stays current.
            var consoleState = GetConsoleState(parent.Value, emitter); // LuaM
            if (emitter.LastConsoleState != consoleState) // LuaM
            {
                emitter.LastConsoleState = consoleState; // LuaM
                _fireControl.RefreshConsolesOnGrid(parent.Value); // ADT-Tweak: shuttle consoles don't show the shield here
            }
            // Forge-Change-End
        }
    }

    // LuaM-start: animate shuttle shield formation and destruction.
    private void UpdateShieldVisuals(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ShipShieldVisualsComponent>();
        while (query.MoveNext(out var uid, out var visuals))
        {
            if (ShipShieldVisualsProgress.IsShatterFinished(visuals, now))
                TryQueueDel(uid);
        }
    }
    // LuaM-end

    public override void Initialize()
    {
        base.Initialize();
        _projectileQuery = GetEntityQuery<ProjectileComponent>();
        _shipWeaponProjectileQuery = GetEntityQuery<ShipWeaponProjectileComponent>();

        SubscribeLocalEvent<ShipShieldComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<ShipShieldEmitterComponent, ComponentShutdown>(OnEmitterShutdown); // Mono

        InitializeCommands();
        InitializeEmitters();
    }

    private void OnPreventCollide(EntityUid uid, ShipShieldComponent component, ref PreventCollideEvent args)
    {
        // only handle ship weapons for now. engine update introduced physics regressions. Let's polish everything else and circle back yeah?
        // Ensuring projectiles coming froms same grid don't hit shield is handled by ProjectileGridPhaseComponent
        if (!_shipWeaponProjectileQuery.HasComponent(args.OtherEntity) ||
        !_projectileQuery.TryGetComponent(args.OtherEntity, out var projectile) ||
        projectile.ProjectileSpent)
        {
            args.Cancelled = true;
            return;
        }

        //if (TryComp<TimedDespawnComponent>(args.OtherEntity, out var despawn))
        //    despawn.Lifetime += despawn.Lifetime;

        // I originally tried reflection but the math is too hard with the fucked coordinate system in this game (WorldRotation can be negative. Vector to Angle conversion loses information. Etc etc.)
        // Might try again at some point using just vector math with this (https://math.stackexchange.com/questions/13261/how-to-get-a-reflection-vector)
        //var deflectionVector = Transform(args.OtherEntity).WorldPosition - Transform(uid).WorldPosition;
        //var angle = _random.NextFloat(DeflectionSpread);

        //if (_random.Prob(0.5f))
        //    angle = -angle;

        //deflectionVector = new Vector2((float) (Math.Cos(angle) * deflectionVector.X - Math.Sin(angle) * deflectionVector.Y), (float) (Math.Sin(angle) * deflectionVector.X - Math.Cos(angle) * deflectionVector.Y));

        // instead of reflecting the projectile, just delete it. this works better for gameplay and intuiting what is going on in a fight.
        // why shoot the projectile again when you can just 180 its physics, tho?
        //_gun.ShootProjectile(args.OtherEntity, deflectionVector, _physicsSystem.GetMapLinearVelocity(uid), uid, null, velocity.Length());

        if (component.Source is { } source)
        {
            var ev = new ShieldDeflectedEvent(args.OtherEntity, projectile);
            RaiseLocalEvent(source, ref ev);
        }
    }

    private static (EntityUid Grid, bool Online, int Percent, bool Overloaded) GetConsoleState(EntityUid grid, ShipShieldEmitterComponent emitter) // LuaM
    {
        var limit = emitter.DamageLimit > 0 ? emitter.DamageLimit : 1f;
        var percent = (int) MathF.Round(Math.Clamp(1f - emitter.Damage / limit, 0f, 1f) * 100f);
        return (grid, emitter.Shield != null, percent, emitter.OverloadAccumulator > 0);
    }

    private void OnEmitterShutdown(EntityUid uid, ShipShieldEmitterComponent emitter, ComponentShutdown args) // Mono
    {
        var parent = Transform(uid).GridUid; // Forge-Change

        if (emitter.Shielded != null)
        {
            UnshieldEntity(emitter.Shielded.Value);
            emitter.Shield = null;
            emitter.Shielded = null;
        }

        // Forge-Change-Start
        // Refresh consoles so the shield HP bar disappears when the emitter is removed from the grid.
        if (parent != null)
            _fireControl.RefreshConsolesOnGrid(parent.Value); // ADT-Tweak: shuttle consoles don't show the shield here
        // Forge-Change-End
    }

    /// <summary>
    /// Produces a shield around a grid entity, if it doesn't already exist.
    /// </summary>
    /// <param name="entity">The entity being shielded.</param>
    /// <param name="mapGrid">The map grid component of the entity being shielded.</param>
    /// <param name="source">A shield generator or similar providing the shield for the entity</param>
    /// <returns>The shield entity.</returns>
    private EntityUid ShieldEntity(EntityUid entity, EntityUid? source = null, MapGridComponent? mapGrid = null)
    {
        if (TryComp<ShipShieldedComponent>(entity, out var existingShielded))
            return existingShielded.Shield;

        if (!Resolve(entity, ref mapGrid, false) || HasComp<ShipShieldDisabledGridComponent>(Transform(entity).GridUid))
            return EntityUid.Invalid;

        var prototype = ShipShieldPrototype;

        var shield = Spawn(prototype, Transform(entity).Coordinates);
        var shieldPhysics = EnsureComp<PhysicsComponent>(shield);
        var shieldComp = EnsureComp<ShipShieldComponent>(shield);
        shieldComp.Shielded = entity;
        shieldComp.Source = source;

        // Copy shield color from the generator to the shield visuals
        var shieldVisuals = EnsureComp<ShipShieldVisualsComponent>(shield);
        // LuaM-start: initialize shader animation state.
        shieldVisuals.FormStart = _timing.CurTime; // LuaM
        shieldVisuals.ShatterStart = null; // LuaM
        if (source != null && TryComp<ShipShieldEmitterComponent>(source.Value, out var emitter))
        {
            var color = emitter.ShieldColor;
            if (color.A >= 1f)
                color = color.WithAlpha(0.92f);
            shieldVisuals.ShieldColor = color;
        }
        Dirty(shield, shieldVisuals);
        // LuaM-end

        var gridCenter = new EntityCoordinates(entity, mapGrid.LocalAABB.Center);
        _transformSystem.SetCoordinates(shield, gridCenter);
        _transformSystem.SetWorldRotation(shield, _transformSystem.GetWorldRotation(entity));

        var chain = GenerateOvalFixture(shield, "shield", shieldPhysics, mapGrid, shieldVisuals.Padding);

        List<Vector2> roughPoly = new();

        var interval = chain.Count / PhysicsConstants.MaxPolygonVertices;

        int i = 0;

        while (i < PhysicsConstants.MaxPolygonVertices)
        {
            roughPoly.Add(chain.Vertices[i * interval]);
            i++;
        }

        var internalPoly = new PolygonShape();
        internalPoly.Set(roughPoly);

        _fixtureSystem.TryCreateFixture(shield, internalPoly, "internalShield",
            hard: true,
            collisionLayer: (int)CollisionGroup.BulletImpassable, // Mono - Only try to block bullets
            body: shieldPhysics);

        _physicsSystem.WakeBody(shield, body: shieldPhysics);
        _physicsSystem.SetSleepingAllowed(shield, shieldPhysics, false);

        _pvsSys.AddGlobalOverride(shield);

        var shieldedComp = EnsureComp<ShipShieldedComponent>(entity);
        shieldedComp.Shield = shield;
        shieldedComp.Source = source;

        return shield;
    }

    private bool UnshieldEntity(EntityUid uid, ShipShieldedComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return false;

        // LuaM-start: keep the visual entity briefly for the shatter animation.
        var shield = component.Shield;
        RemComp<ShipShieldedComponent>(uid);

        if (TryComp<ShipShieldVisualsComponent>(shield, out var visuals) && visuals.ShatterStart == null) // LuaM
        {
            visuals.ShatterStart = _timing.CurTime; // LuaM
            Dirty(shield, visuals);
            SoftenShieldCollision(shield);
            return true;
        }

        TryQueueDel(shield);
        // LuaM-end
        return true;
    }

    // LuaM-start: a shattering shield must no longer block projectiles.
    private void SoftenShieldCollision(EntityUid shield)
    {
        if (!TryComp<FixturesComponent>(shield, out var fixtures) || !TryComp<PhysicsComponent>(shield, out var physics))
            return;

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            if (!fixture.Hard)
                continue;

            _physicsSystem.SetHard(shield, fixture, false, fixtures);
        }

        _physicsSystem.WakeBody(shield, body: physics);
    }
    // LuaM-end

    private ChainShape GenerateOvalFixture(EntityUid uid, string name, PhysicsComponent physics, MapGridComponent mapGrid, float padding)
    {
        float radius;
        float scale;
        var scaleX = true;

        var height = mapGrid.LocalAABB.Height + padding;
        var width = mapGrid.LocalAABB.Width + padding;

        if (width > height)
        {
            radius = 0.5f * height;
            scale = width / height;
        }
        else
        {
            radius = 0.5f * width;
            scale = height / width;
            scaleX = false;
        }

        var chain = new ChainShape();

        Span<Vector2> vertices = stackalloc Vector2[ShieldChainVertices + 1]; // LuaM: CreateLoop(radius) > oval vertices with closing edge
        var arcLength = MathF.PI * 2f / ShieldChainVertices;
        for (var i = 0; i < ShieldChainVertices; i++)
        {
            var vertex = new Vector2(MathF.Cos(arcLength * i) * radius, MathF.Sin(arcLength * i) * radius);
            if (scaleX)
                vertex.X *= scale;
            else
                vertex.Y *= scale;

            vertices[i] = vertex;
        }

        vertices[ShieldChainVertices] = vertices[0];
        chain.CreateLoop(vertices);

        _fixtureSystem.TryCreateFixture(uid, chain, name,
            hard: false,
            collisionLayer: (int)CollisionGroup.BulletImpassable, // Mono - Only blocks bullets
            body: physics);

        return chain;
    }

    [ByRefEvent]
    public record struct ShieldDeflectedEvent(EntityUid Deflected, ProjectileComponent Projectile)
    {

    }
}
