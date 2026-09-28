using Content.Server.Atmos.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared.Atmos;
using Content.Shared.Inventory;
using Content.Shared.Rejuvenate;
using Content.Shared.Temperature;
using Content.Shared.Projectiles;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.HeatContainer;
using Content.Shared.Temperature.Systems;

namespace Content.Server.Temperature.Systems;

public sealed partial class TemperatureSystem : SharedTemperatureSystem
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemperatureComponent, AtmosExposedUpdateEvent>(OnAtmosExposedUpdate);
        SubscribeLocalEvent<TemperatureComponent, RejuvenateEvent>(OnRejuvenate);
        Subs.SubscribeWithRelay<TemperatureProtectionComponent, BeforeHeatExchangeEvent>(OnBeforeHeatExchange, held: false);

        SubscribeLocalEvent<InternalTemperatureComponent, MapInitEvent>(OnInit);

        SubscribeLocalEvent<ChangeTemperatureOnCollideComponent, ProjectileHitEvent>(ChangeTemperatureOnCollide);

        InitializeDamage();
    }

    protected override void OnMapInit(Entity<TemperatureComponent> entity, ref MapInitEvent args)
    {
        base.OnMapInit(entity, ref args);

        // Make sure entities don't spawn cold!
        if (_thermalRegulatorQuery.TryComp(entity, out var comp))
            entity.Comp.Temperature = comp.NormalBodyTemperature;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // conduct heat from the surface to the inside of entities with internal temperatures
        var query = EntityQueryEnumerator<InternalTemperatureComponent, TemperatureComponent>();
        while (query.MoveNext(out var uid, out var comp, out var temp))
        {
            // don't do anything if they equalized
            var diff = Math.Abs(temp.Temperature - comp.Temperature);
            if (diff < 0.1f)
                continue;

            ConductHeat((uid, temp), ref comp, frameTime, comp.Conductance, true);
        }

        UpdateDamage();
    }

<<<<<<< ours
    public void ForceChangeTemperature(EntityUid uid, float temp, TemperatureComponent? temperature = null)
    {
        if (!TemperatureQuery.Resolve(uid, ref temperature))
            return;

        var lastTemp = temperature.CurrentTemperature;
        var delta = temperature.CurrentTemperature - temp;

        // ADT Heretic
        var attemptEv = new TemperatureChangeAttemptEvent(temp, lastTemp, delta);
        RaiseLocalEvent(uid, attemptEv);
        if (attemptEv.Cancelled)
            return;

        temperature.CurrentTemperature = temp;
        RaiseLocalEvent(uid, new OnTemperatureChangeEvent(temperature.CurrentTemperature, lastTemp, delta), broadcast: true);
    }

    public override void ChangeHeat(EntityUid uid, float heatAmount, bool ignoreHeatResistance = false, TemperatureComponent? temperature = null)
    {
        if (!TemperatureQuery.Resolve(uid, ref temperature, false))
            return;

        if (!ignoreHeatResistance)
        {
            var ev = new ModifyChangedTemperatureEvent(heatAmount);
            RaiseLocalEvent(uid, ev);
            heatAmount = ev.TemperatureDelta;
        }

        float lastTemp = temperature.CurrentTemperature;
        var newTemp = temperature.CurrentTemperature + heatAmount / GetHeatCapacity(uid, temperature);
        float delta = newTemp - lastTemp;

        // ADT Heretic
        var attemptEv = new TemperatureChangeAttemptEvent(newTemp, lastTemp, delta);
        RaiseLocalEvent(uid, attemptEv);
        if (attemptEv.Cancelled)
            return;

        temperature.CurrentTemperature = newTemp;

        RaiseLocalEvent(uid, new OnTemperatureChangeEvent(temperature.CurrentTemperature, lastTemp, delta), broadcast: true);
    }

    private void OnAtmosExposedUpdate(EntityUid uid, TemperatureComponent temperature, ref AtmosExposedUpdateEvent args)
||||||| base
    public void ForceChangeTemperature(EntityUid uid, float temp, TemperatureComponent? temperature = null)
    {
        if (!TemperatureQuery.Resolve(uid, ref temperature))
            return;

        var lastTemp = temperature.CurrentTemperature;
        var delta = temperature.CurrentTemperature - temp;
        temperature.CurrentTemperature = temp;
        RaiseLocalEvent(uid, new OnTemperatureChangeEvent(temperature.CurrentTemperature, lastTemp, delta), broadcast: true);
    }

    public override void ChangeHeat(EntityUid uid, float heatAmount, bool ignoreHeatResistance = false, TemperatureComponent? temperature = null)
    {
        if (!TemperatureQuery.Resolve(uid, ref temperature, false))
            return;

        if (!ignoreHeatResistance)
        {
            var ev = new ModifyChangedTemperatureEvent(heatAmount);
            RaiseLocalEvent(uid, ev);
            heatAmount = ev.TemperatureDelta;
        }

        float lastTemp = temperature.CurrentTemperature;
        temperature.CurrentTemperature += heatAmount / GetHeatCapacity(uid, temperature);
        float delta = temperature.CurrentTemperature - lastTemp;

        RaiseLocalEvent(uid, new OnTemperatureChangeEvent(temperature.CurrentTemperature, lastTemp, delta), broadcast: true);
    }

    private void OnAtmosExposedUpdate(EntityUid uid, TemperatureComponent temperature, ref AtmosExposedUpdateEvent args)
=======
    private void OnAtmosExposedUpdate(Entity<TemperatureComponent> entity, ref AtmosExposedUpdateEvent args)
>>>>>>> theirs
    {
        var transform = args.Transform;

        if (transform.MapUid == null)
            return;

        // TODO ATMOS: Atmos heat containers!!!
        var atmosContainer = new HeatContainer(_atmosphere.GetHeatCapacity(args.GasMixture, false), args.GasMixture.Temperature);
        ConductHeat(entity.AsNullable(), ref atmosContainer, args.DeltaTime, args.ConductivityMod);
        args.GasMixture.Temperature = atmosContainer.Temperature;
    }

    private void OnInit(Entity<InternalTemperatureComponent> entity, ref MapInitEvent args)
    {
        if (!TemperatureQuery.TryComp(entity, out var temp))
            return;

        // TODO: This shouldn't copy temperature component, but this component is so niche it's not worth the effort of fixing.
        entity.Comp.Temperature = temp.Temperature;
        entity.Comp.HeatCapacity = temp.HeatCapacity;
    }

    private void OnRejuvenate(Entity<TemperatureComponent> entity, ref RejuvenateEvent args)
    {
        SetTemperature(entity.AsNullable(), _thermalRegulatorQuery.CompOrNull(entity)?.NormalBodyTemperature ?? Atmospherics.T20C);
    }

    private void OnBeforeHeatExchange(Entity<TemperatureProtectionComponent> entity, ref BeforeHeatExchangeEvent args)
    {
        // TODO: Proper coverage modifiers!!! This should be its own system which relays to inventory and then based on coverage spits out a modifier!
        args.HeatTransferModifier *= entity.Comp.Coefficient;
    }

    private void ChangeTemperatureOnCollide(Entity<ChangeTemperatureOnCollideComponent> ent, ref ProjectileHitEvent args)
    {
        ChangeHeat(args.Target, ent.Comp.Heat, ent.Comp.IgnoreHeatResistance);// adjust the temperature
    }
}
