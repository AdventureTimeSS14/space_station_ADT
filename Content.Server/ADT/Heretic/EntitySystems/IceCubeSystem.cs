using System.Linq;
using Content.Shared.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared.ADT.Heretic.Common;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Temperature;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.ADT.Heretic.EntitySystems;

// ADT: from Goob Wizard.Systems, no IceCubeOnProjectileHit

public sealed class IceCubeSystem : SharedIceCubeSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    private const string IceCubeFixture = "ice-cube-fixture";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IceCubeComponent, ComponentStartup>(IceCubeAdded);
        SubscribeLocalEvent<IceCubeComponent, ComponentShutdown>(IceCubeRemoved);
        SubscribeLocalEvent<IceCubeComponent, OnTemperatureChangeEvent>(OnTemperatureChange);
        SubscribeLocalEvent<IceCubeComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<IceCubeComponent, BeforeStaminaDamageEvent>(OnStaminaDamage, before: [typeof(SharedStaminaSystem)]);
    }

    private void OnStaminaDamage(Entity<IceCubeComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        if (args.Value <= 0)
            return;

        if (!TryComp(ent, out TemperatureComponent? temperature))
            return;

        ent.Comp.SustainedDamage += args.Value * ent.Comp.StaminaDamageMeltProbabilityMultiplier;
        if (ShouldUnfreeze(ent, temperature.CurrentTemperature))
            RemCompDeferred(ent, ent.Comp);
    }

    private void OnDamageChanged(Entity<IceCubeComponent> ent, ref DamageChangedEvent args)
    {
        var (uid, comp) = ent;

        if (!TryComp(uid, out TemperatureComponent? temperature))
            return;

        if (args is not { DamageIncreased: true, DamageDelta: not null })
            return;

        if (args.DamageDelta.DamageDict.TryGetValue("Heat", out var heat))
        {
            _temperature.ForceChangeTemperature(uid,
                MathF.Min(comp.UnfreezeTemperatureThreshold + 10f,
                    temperature.CurrentTemperature + heat.Float() * comp.TemperaturePerHeatDamageIncrease),
                temperature);
        }

        var realDamage = args.DamageDelta.DamageDict.Where(kvp => kvp.Key.Id is "Blunt" or "Slash" or "Piercing" or "Heat") // ADT: compare ProtoId by Id
            .Sum(kvp => kvp.Value.Float());

        if (realDamage <= 0f)
            return;

        ent.Comp.SustainedDamage += realDamage * ent.Comp.SustainedDamageMeltProbabilityMultiplier;

        if (ShouldUnfreeze(ent, temperature.CurrentTemperature))
            RemCompDeferred(ent.Owner, ent.Comp);
    }

    private bool ShouldUnfreeze(Entity<IceCubeComponent> ent, float curTemp)
    {
        if (ent.Comp.SustainedDamage <= ent.Comp.DamageMeltProbabilityThreshold)
            return false;

        var probability = Math.Clamp(ent.Comp.SustainedDamage /
            100f * Math.Clamp(InverseLerp(ent.Comp.FrozenTemperature, ent.Comp.UnfrozenTemperature, curTemp), 0.2f, 1f),
            0.2f, // At least 20%
            1f);

        return _random.Prob(probability);
    }

    private float InverseLerp(float min, float max, float value)
    {
        return max <= min ? 1f : Math.Clamp((value - min) / (max - min), 0f , 1f);
    }

    private void OnTemperatureChange(Entity<IceCubeComponent> ent, ref OnTemperatureChangeEvent args)
    {
        if (args.TemperatureDelta > 0f && args.CurrentTemperature > ent.Comp.UnfreezeTemperatureThreshold)
            RemCompDeferred(ent.Owner, ent.Comp);
    }

    private void IceCubeRemoved(Entity<IceCubeComponent> ent, ref ComponentShutdown args)
    {
        var (uid, comp) = ent;

        if (TerminatingOrDeleted(uid))
            return;

        if (TryComp(uid, out TemperatureComponent? temperature))
        {
            _temperature.ForceChangeTemperature(uid,
                MathF.Max(temperature.CurrentTemperature, comp.UnfrozenTemperature),
                temperature);
        }

        _blocker.UpdateCanMove(uid);

        Popup.PopupEntity(Loc.GetString("ice-cube-melt"), uid);

        if (!TryComp(uid, out PhysicsComponent? physics) || !TryComp(uid, out FixturesComponent? fixtures))
            return;

        var xform = Transform(uid);

        var fixture = _fixtures.GetFixtureOrNull(uid, IceCubeFixture, fixtures);

        if (fixture != null)
            _fixtures.DestroyFixture(uid, IceCubeFixture, fixture, body: physics, manager: fixtures, xform: xform);
        else
            _fixtures.FixtureUpdate(uid, manager: fixtures, body: physics);

        if (comp.OldBodyType != null)
            Physics.SetBodyType(uid, comp.OldBodyType.Value, fixtures, physics, xform);
    }

    private void IceCubeAdded(Entity<IceCubeComponent> ent, ref ComponentStartup args)
    {
        var (uid, comp) = ent;

        if (TryComp(uid, out TemperatureComponent? temperature))
        {
            _temperature.ForceChangeTemperature(uid,
                MathF.Min(temperature.CurrentTemperature, comp.FrozenTemperature),
                temperature);
        }

        _blocker.UpdateCanMove(uid);

        if (!TryComp(uid, out PhysicsComponent? physics) || !TryComp(uid, out FixturesComponent? fixtures))
            return;

        var xform = Transform(uid);

        // For whatever reason I can't set bounds on PhysShapeAabb in code so I have to use polygon shape
        var shape = new PolygonShape();
        shape.SetAsBox(new Box2(-0.4f, -0.4f, 0.4f, 0.4f));
        _fixtures.TryCreateFixture(uid,
            shape,
            IceCubeFixture,
            collisionLayer: comp.CollisionLayer,
            collisionMask: comp.CollisionMask,
            restitution: comp.Restitution,
            manager: fixtures,
            body: physics,
            xform: xform);

        if (physics.BodyType != BodyType.KinematicController)
            return;

        comp.OldBodyType = physics.BodyType;
        Physics.SetBodyType(uid, comp.FrozenBodyType, fixtures, physics, xform);
    }
}
