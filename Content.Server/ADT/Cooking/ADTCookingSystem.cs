using Content.Server.Atmos.EntitySystems;
using Content.Shared.ADT.Cooking;
using Content.Shared.ADT.Cooking.Components;
using Content.Shared.ADT.Cooking.Prototypes;
using Content.Shared.ADT.Cooking.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Tag;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Cooking;

public sealed partial class ADTCookingSystem : SharedADTCookingSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedSolutionContainerSystem _solution = default!;
    [Dependency] private SolutionTransferSystem _solutionTransfer = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private TagSystem _tag = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private ADTDishSystem _dish = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;

    private readonly List<Entity<ADTCookwareComponent, ADTActiveCookingComponent>> _toTick = new();
    private readonly List<Entity<ADTStoveComponent>> _finishedOvens = new();
    private TimeSpan _nextOvenTimerCheck;

    private ADTCookingSettingsPrototype Settings => _proto.Index<ADTCookingSettingsPrototype>(ADTCookingSettingsPrototype.DefaultId);

    public override void Initialize()
    {
        base.Initialize();

        InitializeStove();
        InitializeCookware();
        InitializePlate();
        InitializeSauce();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        ProcessPendingServing();

        var now = _timing.CurTime;
        var settings = Settings;
        var interval = TimeSpan.FromSeconds(settings.UpdateInterval);

        _toTick.Clear();
        var query = EntityQueryEnumerator<ADTActiveCookingComponent, ADTCookwareComponent>();
        while (query.MoveNext(out var uid, out var active, out var cookware))
        {
            if (now < active.NextUpdate)
                continue;

            active.NextUpdate += interval;
            if (active.NextUpdate < now)
                active.NextUpdate = now + interval;

            _toTick.Add((uid, cookware, active));
        }

        foreach (var ent in _toTick)
        {
            if (TerminatingOrDeleted(ent))
                continue;

            Tick((ent.Owner, ent.Comp1), ent.Comp2, settings);
        }

        UpdateOvenTimers();
    }

    private void Tick(Entity<ADTCookwareComponent> ent, ADTActiveCookingComponent active, ADTCookingSettingsPrototype settings)
    {
        var comp = ent.Comp;

        if (comp.FireEndTime is { } fireEnd)
        {
            if (_timing.CurTime >= fireEnd)
                EndFire(ent);

            return;
        }

        if (!TryComp<ADTStoveComponent>(active.Stove, out var stove) || active.Slot >= stove.Slots.Count)
        {
            StopActive(ent);
            return;
        }

        var heat = stove.Slots[active.Slot].Heat;
        if (heat == ADTStoveHeat.Off)
        {
            StopActive(ent);
            return;
        }

        if (!_power.IsPowered(active.Stove))
            return;

        var medium = GetMedium(ent);

        if (CheckFlare(ent, heat, medium, settings))
            return;

        if (comp.Stage >= ADTCookingStage.Done && !HasResult(ent))
            ResetCooking(ent);

        UpdateTarget(ent, medium, settings);

        var multiplier = settings.HeatMultipliers.GetValueOrDefault(heat, 1f) * GetSpeed(stove);
        var delta = settings.UpdateInterval * multiplier;

        switch (comp.Stage)
        {
            case ADTCookingStage.Cooking:
                comp.CookTime += delta;
                UpdateMinigames(ent, active.Stove, settings);

                if (comp.CookTime >= comp.TargetTime)
                    Finish(ent, medium, settings);
                break;
            case ADTCookingStage.Done:
            case ADTCookingStage.Overcooked:
                comp.CookTime += delta * GetBurnMultiplier(ent, medium, heat, settings);

                if (comp.CookTime >= comp.TargetTime + comp.BurnTime)
                    Burn(ent, settings);
                else if (comp.Stage == ADTCookingStage.Done && comp.CookTime >= comp.TargetTime + comp.OvercookTime)
                    Overcook(ent, settings);
                break;
            case ADTCookingStage.Burnt:
                if (medium != null && medium.Flammable && medium.SmokeToFire.TryGetValue(heat, out var smokeToFire))
                {
                    comp.SmokeTime += settings.UpdateInterval;
                    if (comp.SmokeTime >= smokeToFire)
                        StartFire(ent, settings);
                }
                break;
        }

        UpdateAppearance(ent);
    }

    private float GetBurnMultiplier(Entity<ADTCookwareComponent> ent, ADTCookingMediumPrototype? medium, ADTStoveHeat heat, ADTCookingSettingsPrototype settings)
    {
        if (ent.Comp.Type == ADTCookwareType.Pan && medium == null)
            return settings.NoMediumBurnMultiplier;

        if (medium != null && medium.BurnMultipliers.TryGetValue(heat, out var burn))
            return burn;

        return 1f;
    }

    private void EnsureActive(EntityUid cookware, EntityUid stove, int slot)
    {
        var active = EnsureComp<ADTActiveCookingComponent>(cookware);
        active.Stove = stove;
        active.Slot = slot;
        active.NextUpdate = _timing.CurTime + TimeSpan.FromSeconds(Settings.UpdateInterval);

        if (!TryComp<ADTCookwareComponent>(cookware, out var comp))
            return;

        RefreshVisualTiming((cookware, comp));
        UpdateAppearance((cookware, comp));
    }

    private void StopActive(Entity<ADTCookwareComponent> ent)
    {
        if (ent.Comp.FireEndTime != null)
            return;

        RemCompDeferred<ADTActiveCookingComponent>(ent);
        UpdateAppearance(ent, false);
        RefreshVisualTiming(ent, false);
    }

    private void RefreshVisualTiming(Entity<ADTCookwareComponent> ent, bool? activeOverride = null)
    {
        var comp = ent.Comp;
        var settings = Settings;
        var rate = 0f;
        var heat = ADTStoveHeat.Off;

        var active = activeOverride ?? HasComp<ADTActiveCookingComponent>(ent);
        if (active && comp.FireEndTime == null && TryGetStoveSlot(ent, out var stove, out var slot) && _power.IsPowered(stove.Owner))
        {
            heat = stove.Comp.Slots[slot].Heat;
            if (heat != ADTStoveHeat.Off)
                rate = settings.HeatMultipliers.GetValueOrDefault(heat, 1f) * GetSpeed(stove.Comp);
        }

        comp.VisualRefTime = _timing.CurTime;
        comp.CookBase = 0f;
        comp.CookRate = 0f;
        comp.BurnBase = 0f;
        comp.BurnRate = 0f;

        switch (comp.Stage)
        {
            case ADTCookingStage.Cooking:
                if (comp.TargetTime > 0)
                {
                    comp.CookBase = Math.Clamp(comp.CookTime / comp.TargetTime, 0f, 1f);
                    comp.CookRate = rate / comp.TargetTime;
                }
                break;
            case ADTCookingStage.Done:
            case ADTCookingStage.Overcooked:
                comp.CookBase = 1f;
                comp.BurnBase = GetBurnFraction(comp);
                if (comp.BurnTime > 0)
                    comp.BurnRate = rate * GetBurnMultiplier(ent, GetMedium(ent), heat, settings) / comp.BurnTime;
                break;
        }

        Dirty(ent);
    }

    private static float GetSpeed(ADTStoveComponent stove)
    {
        return stove.CookTimeMultiplier > 0f ? 1f / stove.CookTimeMultiplier : 1f;
    }

    private void ResetCooking(Entity<ADTCookwareComponent> ent)
    {
        var comp = ent.Comp;
        comp.Stage = ADTCookingStage.Idle;
        comp.TargetKey = null;
        comp.Recipe = null;
        comp.TargetKind = ADTCookingTargetKind.None;
        comp.CookTime = 0;
        comp.TargetTime = 0;
        comp.Addons = 0;
        comp.SmokeTime = 0;
        comp.SoupResult = null;
        comp.SoupName = null;
        comp.SoupQuality = null;
        comp.SoupFlavors.Clear();
        comp.SoupPortions = 0;
        comp.CookResults.Clear();
        comp.CookCenterResult = null;
        comp.CookToppings.Clear();
        comp.CookToppingsStay = false;
        ResetMinigames(comp);
        Dirty(ent);
        UpdateAppearance(ent);
        RefreshVisualTiming(ent);
    }

    private void SetStage(Entity<ADTCookwareComponent> ent, ADTCookingStage stage)
    {
        if (ent.Comp.Stage == stage)
            return;

        ent.Comp.Stage = stage;
        Dirty(ent);
    }

    private static ADTCookingMethod MethodFor(ADTCookwareType type)
    {
        return type switch
        {
            ADTCookwareType.Pan => ADTCookingMethod.Fried,
            ADTCookwareType.Pot => ADTCookingMethod.Boiled,
            ADTCookwareType.Tray => ADTCookingMethod.Baked,
            _ => ADTCookingMethod.Raw,
        };
    }
}
