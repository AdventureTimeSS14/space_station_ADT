using System.Linq;
using Content.Shared.Actions;
using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.Alert;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Revenant.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Chaplain.Systems;

public abstract partial class SharedChaplainSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly EntProtoId BelieverAction = "ActionChaplainBeliever";
    private static readonly EntProtoId TransferAction = "ActionChaplainTransfer";
    private static readonly EntProtoId HolyTouchAction = "ActionChaplainHolyWater";
    private static readonly ProtoId<DamageGroupPrototype> BruteDamageGroup = "Brute";
    private const string CandleId = "Candle";
    private const string InfiniteCandleSuffix = "Infinite";

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

        InitializeHoly();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ChaplainComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Power >= comp.PowerRegenCap)
            {
                comp.NextRegen = now + comp.RegenDelay;
                continue;
            }

            if (now < comp.NextRegen)
                continue;

            comp.NextRegen = now + comp.RegenDelay;
            ChangePowerAmount((uid, comp), 1);
        }

        UpdateSuppression(now);
    }

    private void OnStartup(Entity<ChaplainComponent> ent, ref ComponentStartup args)
    {
        UpdateAlert(ent);
    }

    private void OnMapInit(Entity<ChaplainComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextRegen = _timing.CurTime + ent.Comp.RegenDelay;

        _actions.AddAction(ent.Owner, ref ent.Comp.BelieverActionEntity, BelieverAction);
        _actions.AddAction(ent.Owner, ref ent.Comp.TransferActionEntity, TransferAction);
        _actions.AddAction(ent.Owner, ref ent.Comp.HolyTouchActionEntity, HolyTouchAction);
        Dirty(ent);
    }

    private void OnShutdown(Entity<ChaplainComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.BelieverActionEntity);
        _actions.RemoveAction(ent.Owner, ent.Comp.TransferActionEntity);
        _actions.RemoveAction(ent.Owner, ent.Comp.HolyTouchActionEntity);
        _alerts.ClearAlert(ent.Owner, ent.Comp.Alert);
    }

    public bool ChangePowerAmount(Entity<ChaplainComponent?> ent, FixedPoint2 amount)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.Power + amount < 0)
            return false;

        ent.Comp.Power = FixedPoint2.Min(ent.Comp.Power + amount, ent.Comp.PowerRegenCap);
        Dirty(ent);
        UpdateAlert((ent.Owner, ent.Comp));
        return true;
    }

    public bool TryUseAbility(Entity<ChaplainComponent> ent, FixedPoint2 cost)
    {
        if (ent.Comp.Power < cost)
        {
            _popup.PopupClient(Loc.GetString("chaplain-not-enough-power"), ent, ent);
            return false;
        }

        return ChangePowerAmount((ent.Owner, ent.Comp), -cost);
    }

    private void UpdateAlert(Entity<ChaplainComponent> ent)
    {
        var severity = (short) Math.Clamp(Math.Round(ent.Comp.Power.Float()), 0, 5);
        _alerts.ShowAlert(ent.Owner, ent.Comp.Alert, severity);
    }

    #region Believers

    private void OnMakeBeliever(Entity<ChaplainComponent> ent, ref ChaplainMakeBelieverActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var target = args.Target;

        if (HasComp<ChaplainComponent>(target))
        {
            _popup.PopupClient(Loc.GetString("chaplain-believer-already", ("target", Identity.Entity(target, EntityManager))), ent, ent);
            return;
        }

        if (!HasComp<HumanoidProfileComponent>(target))
            return;

        if (ent.Comp.Believers >= ent.Comp.MaxBelievers)
        {
            _popup.PopupClient(Loc.GetString("chaplain-believer-too-much"), ent, ent);
            return;
        }

        if (!TryUseAbility(ent, ent.Comp.BelieverCost))
            return;

        _popup.PopupClient(Loc.GetString("chaplain-believer-start-self", ("target", Identity.Entity(target, EntityManager))), ent, ent);
        _popup.PopupEntity(Loc.GetString("chaplain-believer-start-target", ("issuer", Identity.Entity(ent, EntityManager))), target, target);

        StartDoAfter(ent, target, new ChaplainMakeBelieverDoAfter());
    }

    private void OnMakeBelieverDoAfter(Entity<ChaplainComponent> ent, ref ChaplainMakeBelieverDoAfter args)
    {
        if (args.Handled || args.Args.Target is not { } target)
            return;

        args.Handled = true;

        if (args.Cancelled)
        {
            ChangePowerAmount((ent.Owner, ent.Comp), ent.Comp.BelieverCost);
            return;
        }

        if (ent.Comp.Believers >= ent.Comp.MaxBelievers)
        {
            _popup.PopupClient(Loc.GetString("chaplain-believer-too-much"), ent, ent);
            return;
        }

        OfferReligion(ent, target);
    }

    protected virtual void OfferReligion(Entity<ChaplainComponent> ent, EntityUid target)
    {
    }

    public void MakeBeliever(Entity<ChaplainComponent> ent, EntityUid target)
    {
        ent.Comp.Believers += 1;
        Dirty(ent);

        var believer = EnsureComp<ChaplainComponent>(target);
        believer.MaxBelievers = 0;
        believer.PowerRegenCap = 2;
        believer.Power = 2;
        believer.PowerPerPray = 1;
        Dirty(target, believer);
        UpdateAlert((target, believer));

        _popup.PopupEntity(Loc.GetString("chaplain-believer-success-self", ("target", Identity.Entity(target, EntityManager))), ent, ent);
        _popup.PopupEntity(Loc.GetString("chaplain-believer-success-target", ("issuer", Identity.Entity(ent, EntityManager))), target, target);
    }

    #endregion

    #region Power transfer

    private void OnPrayersHand(Entity<ChaplainComponent> ent, ref ChaplainPrayersHandActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var target = args.Target;

        if (!CanTransferPower(ent, target))
            return;

        _popup.PopupClient(Loc.GetString("chaplain-transfer-start-self", ("target", Identity.Entity(target, EntityManager))), ent, ent);
        _popup.PopupEntity(Loc.GetString("chaplain-transfer-start-target", ("issuer", Identity.Entity(ent, EntityManager))), target, target);

        StartDoAfter(ent, target, new ChaplainPrayersHandDoAfter());
    }

    private void OnPrayersHandDoAfter(Entity<ChaplainComponent> ent, ref ChaplainPrayersHandDoAfter args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target is not { } target)
            return;

        args.Handled = true;

        if (!CanTransferPower(ent, target) || !TryComp<ChaplainComponent>(target, out var targetChaplain))
            return;

        ChangePowerAmount((target, targetChaplain), ent.Comp.PowerTransfered);
        ChangePowerAmount((ent.Owner, ent.Comp), -ent.Comp.PowerTransfered);

        _popup.PopupClient(Loc.GetString("chaplain-transfer-success-self", ("target", Identity.Entity(target, EntityManager))), ent, ent);
        _popup.PopupEntity(Loc.GetString("chaplain-transfer-success-target", ("issuer", Identity.Entity(ent, EntityManager))), target, target);
    }

    private bool CanTransferPower(Entity<ChaplainComponent> ent, EntityUid target)
    {
        if (!TryComp<ChaplainComponent>(target, out var targetChaplain))
        {
            _popup.PopupClient(Loc.GetString("chaplain-transfer-not-chaplain", ("target", Identity.Entity(target, EntityManager))), ent, ent);
            return false;
        }

        if (targetChaplain.Power >= targetChaplain.PowerRegenCap)
        {
            _popup.PopupClient(Loc.GetString("chaplain-transfer-full", ("target", Identity.Entity(target, EntityManager))), ent, ent);
            return false;
        }

        if (ent.Comp.Power < ent.Comp.PowerTransfered)
        {
            _popup.PopupClient(Loc.GetString("chaplain-not-enough-power"), ent, ent);
            return false;
        }

        return true;
    }

    #endregion

    #region Holy touch

    private void OnHolyTouch(Entity<ChaplainComponent> ent, ref ChaplainHolyTouchActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var target = args.Target;

        if (ent.Comp.Power < ent.Comp.HolyWaterCost)
        {
            _popup.PopupClient(Loc.GetString("chaplain-not-enough-power"), ent, ent);
            return;
        }

        if (TryBlessCandle(ent, target))
            return;

        if ((HasComp<SolutionComponent>(target) || HasComp<SolutionManagerComponent>(target)) && !HasComp<BodyComponent>(target))
        {
            BlessSolutions(ent, target);
            return;
        }

        if (HasComp<RevenantComponent>(target))
        {
            var brute = _proto.Index(BruteDamageGroup);
            _damageable.TryChangeDamage(target, new DamageSpecifier(brute, ent.Comp.RevenantDamage), origin: ent);
            _damageable.TryChangeDamage(ent.Owner, new DamageSpecifier(brute, ent.Comp.RevenantSelfDamage), origin: ent);
            _popup.PopupClient(Loc.GetString("chaplain-cursed-touch"), ent, ent, PopupType.LargeCaution);
            return;
        }

        _popup.PopupClient(Loc.GetString("chaplain-holy-touch-fail-self", ("target", Identity.Entity(target, EntityManager))), ent, ent);
        _popup.PopupEntity(Loc.GetString("chaplain-holy-touch-fail-target", ("issuer", Identity.Entity(ent, EntityManager))), target, target);
    }

    private bool TryBlessCandle(Entity<ChaplainComponent> ent, EntityUid target)
    {
        if (MetaData(target).EntityPrototype is not { } proto)
            return false;

        if (!proto.ID.Contains(CandleId) || proto.ID.EndsWith(InfiniteCandleSuffix))
            return false;

        var blessedId = proto.ID + InfiniteCandleSuffix;
        if (!_proto.HasIndex<EntityPrototype>(blessedId))
        {
            _popup.PopupClient(Loc.GetString("chaplain-holy-touch-fail-self", ("target", Identity.Entity(target, EntityManager))), ent, ent);
            return true;
        }

        var xform = Transform(target);
        var candle = PredictedSpawnAtPosition(blessedId, xform.Coordinates);
        _transform.SetLocalRotation(candle, xform.LocalRotation);
        PredictedDel(target);

        ChangePowerAmount((ent.Owner, ent.Comp), -ent.Comp.HolyWaterCost);
        _popup.PopupClient(Loc.GetString("chaplain-candle-transform-success", ("target", Identity.Entity(candle, EntityManager))), ent, ent);
        _audio.PlayPredicted(ent.Comp.HolyWaterSoundPath, ent, ent);
        return true;
    }

    private void BlessSolutions(Entity<ChaplainComponent> ent, EntityUid target)
    {
        var success = false;
        foreach (var (_, soln) in _solutionContainer.EnumerateSolutions(target).ToList())
        {
            var replaced = ReplaceReagent(soln.Comp.Solution, ent.Comp.WaterSolution, ent.Comp.WaterReplaceSolution);
            replaced |= ReplaceReagent(soln.Comp.Solution, ent.Comp.BloodSolution, ent.Comp.BloodReplaceSolution);

            if (!replaced)
                continue;

            success = true;
            _solutionContainer.UpdateChemicals(soln, false);
        }

        if (!success)
        {
            _popup.PopupClient(Loc.GetString("chaplain-holy-water-nothing", ("target", Identity.Entity(target, EntityManager))), ent, ent);
            return;
        }

        ChangePowerAmount((ent.Owner, ent.Comp), -ent.Comp.HolyWaterCost);
        _popup.PopupClient(Loc.GetString("chaplain-holy-water-success", ("target", Identity.Entity(target, EntityManager))), ent, ent);
        _audio.PlayPredicted(ent.Comp.HolyWaterSoundPath, ent, ent);
    }

    private static bool ReplaceReagent(Solution solution, ProtoId<ReagentPrototype> from, ProtoId<ReagentPrototype> to)
    {
        var quantity = solution.GetTotalPrototypeQuantity(from);
        if (quantity == FixedPoint2.Zero)
            return false;

        solution.RemoveReagent(from.Id, quantity);
        solution.AddReagent(to.Id, quantity);
        return true;
    }

    #endregion

    private void StartDoAfter(Entity<ChaplainComponent> ent, EntityUid target, DoAfterEvent ev)
    {
        var doAfter = new DoAfterArgs(EntityManager, ent, ent.Comp.MakeBelieverDuration, ev, ent, target: target)
        {
            DistanceThreshold = 2,
            BreakOnMove = true,
            BreakOnDamage = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };

        _doAfter.TryStartDoAfter(doAfter);
    }
}
