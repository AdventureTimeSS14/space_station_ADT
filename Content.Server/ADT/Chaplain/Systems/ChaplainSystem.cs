using Content.Server.Popups;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Bible.Components;
using Content.Shared.Damage;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Content.Shared.FixedPoint;
using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Server.Chemistry.Containers.EntitySystems;
using Robust.Shared.Prototypes;
using Content.Shared.ADT.Phantom.Components;
using Content.Shared.Revenant.Components;
using Content.Shared.Body.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Humanoid;
using Content.Server.EUI;
using Content.Shared.Mind;
using Content.Server.Chaplain;
using Robust.Shared.Player;

namespace Content.Server.Bible;

public sealed class ChaplainSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly InventorySystem _invSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly EuiManager _euiManager = null!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChaplainComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChaplainComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChaplainComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ChaplainComponent, ChaplainMakeBelieverActionEvent>(OnMakeBeliever);
        SubscribeLocalEvent<ChaplainComponent, ChaplainMakeBelieverDoAfter>(OnMakeBelieverDoAfter);

        SubscribeLocalEvent<ChaplainComponent, ChaplainPrayersHandActionEvent>(OnPrayersHand);
        SubscribeLocalEvent<ChaplainComponent, ChaplainPrayersHandDoAfter>(OnPrayersHandDoAfter);

        SubscribeLocalEvent<ChaplainComponent, ChaplainHolyTouchActionEvent>(OnHolyTouch);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ChaplainComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Power >= comp.PowerRegenCap)
            {
                comp.Accumulator = 0f;
                continue;
            }


            comp.Accumulator += frameTime;

            if (comp.Accumulator <= comp.RegenDelay)
                continue;

            comp.Accumulator -= comp.RegenDelay;

            if (comp.Power < comp.PowerRegenCap)
            {
                ChangePowerAmount(uid, 1, comp);
            }
        }
    }

    private void OnStartup(EntityUid uid, ChaplainComponent component, ComponentStartup args)
    {
        //update the icon
        ChangePowerAmount(uid, 0, component);
    }

    private void OnMapInit(EntityUid uid, ChaplainComponent component, MapInitEvent args)
    {
        _actionsSystem.AddAction(uid, ref component.BelieverActionEntity, "ActionChaplainBeliever");
        _actionsSystem.AddAction(uid, ref component.TransferActionEntity, "ActionChaplainTransfer");
        _actionsSystem.AddAction(uid, ref component.HolyTouchActionEntity, "ActionChaplainHolyWater");
    }

    private void OnShutdown(EntityUid uid, ChaplainComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.BelieverActionEntity);
        _actionsSystem.RemoveAction(uid, component.TransferActionEntity);
        _actionsSystem.RemoveAction(uid, component.HolyTouchActionEntity);
    }

    public bool ChangePowerAmount(EntityUid uid, FixedPoint2 amount, ChaplainComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.Power + amount < 0)
            return false;

        component.Power += amount;

        component.Power = FixedPoint2.Min(component.Power, component.PowerRegenCap);

        _alerts.ShowAlert(uid, _proto.Index(component.Alert), (short) Math.Clamp(Math.Round(component.Power.Float()), 0, 5));

        return true;
    }

    public bool TryUseAbility(EntityUid uid, ChaplainComponent component, FixedPoint2 abilityCost)
    {
        var cost = -abilityCost;
        if (component.Power < abilityCost)
        {
            _popupSystem.PopupEntity(Loc.GetString("chaplain-not-enough-power"), uid, uid);
            return false;
        }

        ChangePowerAmount(uid, cost, component);

        return true;
    }

    private void OnMakeBeliever(EntityUid uid, ChaplainComponent component, ChaplainMakeBelieverActionEvent args)
    {
        var target = args.Target;
        if (args.Handled)
            return;

        args.Handled = true;

        if (!TryUseAbility(uid, component, component.BelieverCost))
            return;

        if (HasComp<ChaplainComponent>(target))
        {
            _popupSystem.PopupEntity(Loc.GetString("chaplain-believer-already", ("target", Identity.Entity(target, EntityManager))), uid, uid);
            return;
        }

        if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            return;
        }

        if (component.Believers >= component.MaxBelievers)
        {
            _popupSystem.PopupEntity(Loc.GetString("chaplain-believer-too-much"), uid, uid);
            return;
        }

        _popupSystem.PopupEntity(Loc.GetString("chaplain-believer-start-self", ("target", Identity.Entity(target, EntityManager))), uid, uid);
        _popupSystem.PopupEntity(Loc.GetString("chaplain-believer-start-target", ("issuer", Identity.Entity(uid, EntityManager))), target, target);

        var doAfter = new DoAfterArgs(EntityManager, uid, component.MakeBelieverDuration, new ChaplainMakeBelieverDoAfter(), uid, target: target)
        {
            DistanceThreshold = 2,
            BreakOnMove = true,
            BreakOnDamage = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };

        _doAfter.TryStartDoAfter(doAfter);

    }

    private void OnMakeBelieverDoAfter(EntityUid uid, ChaplainComponent component, ChaplainMakeBelieverDoAfter args)
    {
        if (args.Handled || args.Args.Target == null)
            return;
        if (args.Cancelled)
        {
            ChangePowerAmount(uid, component.BelieverCost, component);
            return;
        }
        if (component.Believers >= component.MaxBelievers)
        {
            _popupSystem.PopupEntity(Loc.GetString("chaplain-believer-too-much"), uid, uid);
            return;
        }

        args.Handled = true;
        var target = args.Args.Target.Value;
        if (_mindSystem.TryGetMind(target, out var mindId, out var mind) && _player.TryGetSessionById(mind.UserId, out var session))
            _euiManager.OpenEui(new AcceptReligionEui(uid, target, component, this), session);
    }

    public void MakeBeliever(EntityUid uid, EntityUid target, ChaplainComponent component)
    {
        component.Believers += 1;
        var newComponent = EnsureComp<ChaplainComponent>(target);
        newComponent.MaxBelievers = 0;
        newComponent.PowerRegenCap = 2;
        newComponent.Power = 2;
        newComponent.TransferDuration = component.TransferDuration * 2;
        newComponent.PowerPerPray = 1;
        ChangePowerAmount(target, 0, newComponent);

        _popupSystem.PopupEntity(Loc.GetString("chaplain-believer-success-self", ("target", Identity.Entity(target, EntityManager))), uid, uid);
        _popupSystem.PopupEntity(Loc.GetString("chaplain-believer-success-target", ("issuer", Identity.Entity(uid, EntityManager))), target, target);
    }
    private void OnPrayersHand(EntityUid uid, ChaplainComponent component, ChaplainPrayersHandActionEvent args)
    {
        var target = args.Target;
        if (args.Handled)
            return;

        args.Handled = true;

        if (!TryComp<ChaplainComponent>(target, out var chaplain))
        {
            _popupSystem.PopupEntity(Loc.GetString("chaplain-transfer-not-chaplain", ("target", Identity.Entity(target, EntityManager))), uid, uid);
            return;
        }

        if (chaplain.Power >= chaplain.PowerRegenCap)
        {
            _popupSystem.PopupEntity(Loc.GetString("chaplain-transfer-full", ("target", Identity.Entity(target, EntityManager))), uid, uid);
            return;
        }

        if (component.Power < component.PowerTransfered)
        {
            _popupSystem.PopupEntity(Loc.GetString("chaplain-not-enough-power"), uid, uid);
            return;
        }

        _popupSystem.PopupEntity(Loc.GetString("chaplain-transfer-start-self", ("target", Identity.Entity(target, EntityManager))), uid, uid);
        _popupSystem.PopupEntity(Loc.GetString("chaplain-transfer-start-target", ("issuer", Identity.Entity(uid, EntityManager))), target, target);

        var doAfter = new DoAfterArgs(EntityManager, uid, component.MakeBelieverDuration, new ChaplainPrayersHandDoAfter(), uid, target: target)
        {
            DistanceThreshold = 2,
            BreakOnMove = true,
            BreakOnDamage = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };

        _doAfter.TryStartDoAfter(doAfter);

    }

    private void OnPrayersHandDoAfter(EntityUid uid, ChaplainComponent component, ChaplainPrayersHandDoAfter args)
    {
        if (args.Handled || args.Args.Target == null)
            return;

        args.Handled = true;
        var target = args.Args.Target.Value;

        if (!TryComp<ChaplainComponent>(target, out var chaplain))
        {
            _popupSystem.PopupEntity(Loc.GetString("chaplain-transfer-not-chaplain", ("target", Identity.Entity(target, EntityManager))), uid, uid);
            return;
        }

        if (chaplain.Power >= chaplain.PowerRegenCap)
        {
            _popupSystem.PopupEntity(Loc.GetString("chaplain-transfer-full", ("target", Identity.Entity(target, EntityManager))), uid, uid);
            return;
        }

        if (component.Power < component.PowerTransfered)
        {
            _popupSystem.PopupEntity(Loc.GetString("chaplain-not-enough-power"), uid, uid);
            return;
        }

        ChangePowerAmount(target, component.PowerTransfered, chaplain);
        ChangePowerAmount(uid, -component.PowerTransfered, component);


        _popupSystem.PopupEntity(Loc.GetString("chaplain-transfer-success-self", ("target", Identity.Entity(target, EntityManager))), uid, uid);
        _popupSystem.PopupEntity(Loc.GetString("chaplain-transfer-success-target", ("issuer", Identity.Entity(uid, EntityManager))), target, target);
    }
    public ProtoId<DamageGroupPrototype> BruteDamageGroup = "Brute";
    private void OnHolyTouch(EntityUid uid, ChaplainComponent component, ChaplainHolyTouchActionEvent args)
    {
        var target = args.Target;
        if (args.Handled)
            return;

        args.Handled = true;

        if (component.Power < component.HolyWaterCost)
        {
            _popupSystem.PopupEntity(Loc.GetString("chaplain-not-enough-power"), uid, uid);
            return;
        }

        if (TryComp<MetaDataComponent>(target, out var metaData) && metaData.EntityPrototype != null)
        {
            var entityId = metaData.EntityPrototype.ID;

            if (entityId.Contains("Candle") && !entityId.EndsWith("Infinite"))
            {
                var newPrototypeId = entityId + "Infinite";
                if (!_proto.HasIndex<EntityPrototype>(newPrototypeId))
                {
                    _popupSystem.PopupEntity(
                        Loc.GetString("chaplain-holy-touch-fail-self", ("target", Identity.Entity(target, EntityManager))),
                        uid,
                        uid);
                    return;
                }
                var transform = Transform(target);
                var newCandle = Spawn(newPrototypeId, transform.Coordinates);

                var newTransform = Transform(newCandle);
                newTransform.LocalRotation = transform.LocalRotation;

                Del(target);
                ChangePowerAmount(uid, -component.HolyWaterCost, component);
                _popupSystem.PopupEntity(Loc.GetString("chaplain-candle-transform-success", ("target", Identity.Entity(newCandle, EntityManager))), uid, uid);
                _audio.PlayPvs(component.HolyWaterSoundPath, uid);
                return;
            }
        }

        if (TryComp<SolutionContainerManagerComponent>(target, out var solutionContainer) && solutionContainer.Containers != null && !HasComp<BodyComponent>(target))
        {
            bool success = false;
            foreach (var sol in solutionContainer.Containers)
            {
                if (_solutionContainer.TryGetSolution((target, solutionContainer), sol, out var soln, out var solution))
                {
                    var water = component.WaterSolution;
                    var blood = component.BloodSolution;
                    var waterQuantity = solution.GetTotalPrototypeQuantity(water);
                    var bloodQuantity = solution.GetTotalPrototypeQuantity(blood);
                    if (waterQuantity != FixedPoint2.Zero)
                    {
                        solution.RemoveReagent(water, waterQuantity);
                        solution.AddReagent(component.WaterReplaceSolution, waterQuantity);
                        success = true;
                    }
                    if (bloodQuantity != FixedPoint2.Zero)
                    {
                        solution.RemoveReagent(blood, bloodQuantity);
                        solution.AddReagent(component.BloodReplaceSolution, bloodQuantity);
                        success = true;
                    }
                    _solutionContainer.UpdateChemicals(soln.Value, false);
                }
            }

            if (success)
            {
                ChangePowerAmount(uid, -component.HolyWaterCost, component);
                _popupSystem.PopupEntity(Loc.GetString("chaplain-holy-water-success", ("target", Identity.Entity(target, EntityManager))), uid, uid);
                _audio.PlayPvs(component.HolyWaterSoundPath, uid);
            }
            else
            {
                _popupSystem.PopupEntity(Loc.GetString("chaplain-holy-water-nothing", ("target", Identity.Entity(target, EntityManager))), uid, uid);
            }
            return;
        }

        if (HasComp<PhantomComponent>(target) || HasComp<RevenantComponent>(target))
        {
            var damage = new DamageSpecifier(_proto.Index(BruteDamageGroup), 50);
            var damageSelf = new DamageSpecifier(_proto.Index(BruteDamageGroup), 10);
            _damageableSystem.TryChangeDamage(target, damage);
            _damageableSystem.TryChangeDamage(uid, damageSelf);
            _popupSystem.PopupEntity(Loc.GetString("chaplain-cursed-touch"), uid, uid, PopupType.LargeCaution);
        }
        else
        {
            _popupSystem.PopupEntity(Loc.GetString("chaplain-holy-touch-fail-self", ("target", Identity.Entity(target, EntityManager))), uid, uid);
            _popupSystem.PopupEntity(Loc.GetString("chaplain-holy-touch-fail-target", ("issuer", Identity.Entity(uid, EntityManager))), target, target);
        }
    }
}
