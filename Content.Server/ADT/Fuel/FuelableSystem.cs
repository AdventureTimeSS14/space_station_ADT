using Content.Server.Atmos.EntitySystems;
using Content.Server.Stack;
using Content.Shared.ADT.Fuel;
using Content.Shared.Atmos.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Nutrition.Components;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Server.ADT.Fuel;

public sealed class FuelableSystem : EntitySystem
{
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly PointLightSystem _pointLight = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly StackSystem _stacks = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FuelableComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FuelableComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<FuelableComponent, FuelableInsertDoAfterEvent>(OnInsertDoAfter);
    }

    private void OnMapInit(Entity<FuelableComponent> ent, ref MapInitEvent args)
    {
        if (_pointLight.TryGetLight(ent, out var pointLight))
        {
            if (ent.Comp.BaseLightEnergy <= 0f)
                ent.Comp.BaseLightEnergy = pointLight.Energy;
            if (ent.Comp.BaseLightRadius <= 0f)
                ent.Comp.BaseLightRadius = pointLight.Radius;
        }

        UpdateFireStacksFromFuel(ent);
        UpdateLight(ent);
    }

    private void OnInteractUsing(Entity<FuelableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp(ent, out OpenableComponent? openable) && !openable.Opened)
        {
            args.Handled = true;
            return;
        }

        if (!TryComp(args.Used, out PhysicalCompositionComponent? composition) || composition == null)
            return;

        if (!TryComp(args.Used, out MaterialComponent? _))
            return;

        if (!TryComputeUnitFuelSeconds(ent.Comp, composition, out var unitFuelSeconds))
            return;

        if (unitFuelSeconds <= 0.001f)
            return;

        if (ent.Comp.FuelSeconds >= ent.Comp.FuelCapacitySeconds - 0.01f)
        {
            _popup.PopupEntity(Loc.GetString("adt-fuelable-full"), ent, args.User);
            args.Handled = true;
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, TimeSpan.FromSeconds(ent.Comp.InsertDelay),
            new FuelableInsertDoAfterEvent(), ent, used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnInsertDoAfter(Entity<FuelableComponent> ent, ref FuelableInsertDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        var used = args.Used;
        if (used == null || Deleted(used.Value) || !TryComp(used.Value, out PhysicalCompositionComponent? composition) || composition == null)
            return;

        if (!TryComp(used.Value, out MaterialComponent? _))
            return;

        if (!TryComputeUnitFuelSeconds(ent.Comp, composition, out var unitFuelSeconds) || unitFuelSeconds <= 0.001f)
            return;

        var remainingCapacity = MathF.Max(0f, ent.Comp.FuelCapacitySeconds - ent.Comp.FuelSeconds);
        if (remainingCapacity <= 0.01f)
            return;

        var stackCount = 1;
        if (TryComp(used.Value, out StackComponent? stack))
            stackCount = stack.Count;

        var maxUnitsByCapacity = (int)MathF.Floor(remainingCapacity / unitFuelSeconds);
        if (maxUnitsByCapacity <= 0)
            maxUnitsByCapacity = 1;

        var unitsToConsume = Math.Min(stackCount, maxUnitsByCapacity);

        var fuelToAdd = unitsToConsume * unitFuelSeconds;
        ent.Comp.FuelSeconds = MathF.Min(ent.Comp.FuelCapacitySeconds, ent.Comp.FuelSeconds + fuelToAdd);
        Dirty(ent);

        if (stack != null)
        {
            var newCount = stackCount - unitsToConsume;
            if (newCount <= 0)
                QueueDel(used.Value);
            else
                _stacks.SetCount(used.Value, newCount, stack);
        }
        else
        {
            QueueDel(used.Value);
        }

        UpdateFireStacksFromFuel(ent);
        UpdateLight(ent);

        _popup.PopupEntity(Loc.GetString("adt-fuelable-inserted"), ent, args.User);
        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FuelableComponent, FlammableComponent>();
        while (query.MoveNext(out var uid, out var fuelable, out var flammable))
        {
            if (fuelable.FuelSeconds <= 0f)
            {
                if (flammable.FireStacks > 0f)
                    _flammable.SetFireStacks(uid, 0f, flammable);

                UpdateLight((uid, fuelable));
                continue;
            }

            if (flammable.OnFire)
            {
                fuelable.FuelSeconds = MathF.Max(0f, fuelable.FuelSeconds - fuelable.BurnRate * frameTime);
                Dirty(uid, fuelable);
            }

            var stacks = FuelToFireStacks(fuelable);
            _flammable.SetFireStacks(uid, stacks, flammable);

            UpdateLight((uid, fuelable));
        }
    }

    private float FuelToFireStacks(FuelableComponent fuelable)
    {
        if (fuelable.FuelCapacitySeconds <= 0.001f)
            return 0f;

        var frac = Math.Clamp(fuelable.FuelSeconds / fuelable.FuelCapacitySeconds, 0f, 1f);
        return frac * fuelable.MaxFireStacks;
    }

    private void UpdateFireStacksFromFuel(Entity<FuelableComponent> ent)
    {
        if (!TryComp(ent, out FlammableComponent? flammable))
            return;

        _flammable.SetFireStacks(ent, FuelToFireStacks(ent.Comp), flammable);
    }

    private void UpdateLight(Entity<FuelableComponent> ent)
    {
        if (!TryComp(ent, out FlammableComponent? flammable))
            return;

        if (!_pointLight.TryGetLight(ent, out var pointLight))
            return;

        if (!flammable.OnFire || ent.Comp.FuelCapacitySeconds <= 0.001f)
        {
            _pointLight.SetEnabled(ent, false);
            return;
        }

        var frac = Math.Clamp(ent.Comp.FuelSeconds / ent.Comp.FuelCapacitySeconds, 0f, 1f);
        var scaled = 0.2f + 0.8f * frac;

        _pointLight.SetEnabled(ent, true);
        var baseEnergy = ent.Comp.BaseLightEnergy > 0f ? ent.Comp.BaseLightEnergy : pointLight.Energy;
        var baseRadius = ent.Comp.BaseLightRadius > 0f ? ent.Comp.BaseLightRadius : pointLight.Radius;
        _pointLight.SetEnergy(ent, baseEnergy * scaled);
        _pointLight.SetRadius(ent, baseRadius * (0.6f + 0.4f * frac));
    }

    private bool TryComputeUnitFuelSeconds(FuelableComponent fuelable,
        PhysicalCompositionComponent composition,
        out float unitFuelSeconds)
    {
        unitFuelSeconds = 0f;

        if (fuelable.FuelTimePerMaterialUnit.Count == 0)
            return false;

        foreach (var (mat, vol) in composition.MaterialComposition)
        {
            if (!fuelable.FuelTimePerMaterialUnit.TryGetValue(mat, out var secPerUnit))
                continue;

            if (secPerUnit <= 0f)
                continue;

            unitFuelSeconds += vol * secPerUnit;
        }

        return unitFuelSeconds > 0.001f;
    }
}

