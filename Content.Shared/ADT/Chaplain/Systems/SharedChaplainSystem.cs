using System.Linq;
using Content.Shared.Actions;
using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.ADT.HolyDamage;
using Content.Shared.Alert;
using Content.Shared.Body;
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
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
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
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly EntProtoId BelieverAction = "ActionChaplainBeliever";
    private static readonly EntProtoId ShieldAction = "ActionChaplainHolyShield";
    private static readonly EntProtoId HolyTouchAction = "ActionChaplainHolyWater";
    private static readonly EntProtoId ExorcismAction = "ActionChaplainExorcism";
    private static readonly EntProtoId BlessingAction = "ActionChaplainBlessing";
    private static readonly EntProtoId ExorcismSlowdownEffect = "BasicSlowdownStatusEffect";
    private static readonly ProtoId<DamageGroupPrototype> BruteDamageGroup = "Brute";
    private const string CandleId = "Candle";
    private const string InfiniteCandleSuffix = "Infinite";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChaplainComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChaplainComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChaplainComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ChaplainFollowerComponent, ComponentShutdown>(OnFollowerShutdown);

        SubscribeLocalEvent<ChaplainComponent, ChaplainMakeBelieverActionEvent>(OnMakeBeliever);
        SubscribeLocalEvent<ChaplainComponent, ChaplainMakeBelieverDoAfter>(OnMakeBelieverDoAfter);
        SubscribeLocalEvent<ChaplainComponent, ChaplainHolyShieldActionEvent>(OnHolyShield);
        SubscribeLocalEvent<ChaplainComponent, ChaplainHolyTouchActionEvent>(OnHolyTouch);
        SubscribeLocalEvent<ChaplainComponent, ChaplainExorcismActionEvent>(OnExorcism);
        SubscribeLocalEvent<ChaplainComponent, ChaplainBlessingActionEvent>(OnBlessing);

        InitializeHoly();
        InitializePrayers();
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
            if (now < comp.NextRegen)
                continue;

            comp.NextRegen = now + comp.RegenDelay;

            if (comp.Faith < comp.MaxFaith)
                ChangeFaith((uid, comp), comp.BaseRegen + comp.FollowerRegen * CountLivingFollowers(comp));
        }

        UpdateSuppression(now);
        UpdateShields(now);
    }

    private void OnStartup(Entity<ChaplainComponent> ent, ref ComponentStartup args)
    {
        UpdateAlert(ent);
    }

    private void OnMapInit(Entity<ChaplainComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextRegen = _timing.CurTime + ent.Comp.RegenDelay;

        _actions.AddAction(ent.Owner, ref ent.Comp.BelieverActionEntity, BelieverAction);
        _actions.AddAction(ent.Owner, ref ent.Comp.ShieldActionEntity, ShieldAction);
        _actions.AddAction(ent.Owner, ref ent.Comp.HolyTouchActionEntity, HolyTouchAction);
        _actions.AddAction(ent.Owner, ref ent.Comp.ExorcismActionEntity, ExorcismAction);
        _actions.AddAction(ent.Owner, ref ent.Comp.BlessingActionEntity, BlessingAction);
        Dirty(ent);
    }

    private void OnShutdown(Entity<ChaplainComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.BelieverActionEntity);
        _actions.RemoveAction(ent.Owner, ent.Comp.ShieldActionEntity);
        _actions.RemoveAction(ent.Owner, ent.Comp.HolyTouchActionEntity);
        _actions.RemoveAction(ent.Owner, ent.Comp.ExorcismActionEntity);
        _actions.RemoveAction(ent.Owner, ent.Comp.BlessingActionEntity);
        _alerts.ClearAlert(ent.Owner, ent.Comp.Alert);

        foreach (var follower in ent.Comp.Followers)
        {
            if (!TryComp<ChaplainFollowerComponent>(follower, out var followerComp) || followerComp.Chaplain != ent.Owner)
                continue;

            followerComp.Chaplain = null;
            Dirty(follower, followerComp);
        }
    }

    private void OnFollowerShutdown(Entity<ChaplainFollowerComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<ChaplainComponent>(ent.Comp.Chaplain, out var chaplain))
            return;

        chaplain.Followers.Remove(ent.Owner);
        Dirty(ent.Comp.Chaplain.Value, chaplain);
    }

    public int CountLivingFollowers(ChaplainComponent chaplain)
    {
        var count = 0;
        foreach (var follower in chaplain.Followers)
        {
            if (Exists(follower) && !_mobState.IsDead(follower))
                count++;
        }

        return count;
    }

    public bool HasFaith(EntityUid uid)
    {
        return TryComp<ChaplainComponent>(uid, out var chaplain) && chaplain.Faith > 0;
    }

    public float GetHolyMultiplier(EntityUid? uid)
    {
        if (!TryComp<ChaplainComponent>(uid, out var chaplain) || chaplain.Faith > 0)
            return 1f;

        return chaplain.WeakenedHolyMultiplier;
    }

    public bool ChangeFaith(Entity<ChaplainComponent?> ent, FixedPoint2 amount)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.Faith + amount < 0)
            return false;

        ent.Comp.Faith = FixedPoint2.Min(ent.Comp.Faith + amount, ent.Comp.MaxFaith);
        Dirty(ent);
        UpdateAlert((ent.Owner, ent.Comp));
        return true;
    }

    public bool TryUseFaith(Entity<ChaplainComponent> ent, FixedPoint2 cost)
    {
        if (ent.Comp.Faith < cost)
        {
            _popup.PopupClient(Loc.GetString("chaplain-not-enough-power"), ent, ent);
            return false;
        }

        return ChangeFaith((ent.Owner, ent.Comp), -cost);
    }

    private void UpdateAlert(Entity<ChaplainComponent> ent)
    {
        var ratio = ent.Comp.MaxFaith > 0 ? (ent.Comp.Faith / ent.Comp.MaxFaith).Float() : 0f;
        var severity = (short) Math.Clamp(MathF.Round(ratio * 5f), 0, 5);
        _alerts.ShowAlert(ent.Owner, ent.Comp.Alert, severity);
    }

    #region Followers

    private void OnMakeBeliever(Entity<ChaplainComponent> ent, ref ChaplainMakeBelieverActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var target = args.Target;

        if (HasComp<ChaplainComponent>(target) || HasComp<ChaplainFollowerComponent>(target))
        {
            _popup.PopupClient(Loc.GetString("chaplain-believer-already", ("target", Identity.Entity(target, EntityManager))), ent, ent);
            return;
        }

        if (!HasComp<HumanoidProfileComponent>(target))
            return;

        if (HasComp<MindShieldComponent>(target))
        {
            _popup.PopupClient(Loc.GetString("chaplain-believer-mindshield", ("target", Identity.Entity(target, EntityManager))), ent, ent);
            return;
        }

        if (ent.Comp.Followers.Count >= ent.Comp.MaxFollowers)
        {
            _popup.PopupClient(Loc.GetString("chaplain-believer-too-much"), ent, ent);
            return;
        }

        if (!TryUseFaith(ent, ent.Comp.BelieverCost))
            return;

        _popup.PopupClient(Loc.GetString("chaplain-believer-start-self", ("target", Identity.Entity(target, EntityManager))), ent, ent);
        _popup.PopupEntity(Loc.GetString("chaplain-believer-start-target", ("issuer", Identity.Entity(ent, EntityManager))), target, target);

        var doAfter = new DoAfterArgs(EntityManager, ent, ent.Comp.MakeBelieverDuration, new ChaplainMakeBelieverDoAfter(), ent, target: target)
        {
            DistanceThreshold = 2,
            BreakOnMove = true,
            BreakOnDamage = true,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnMakeBelieverDoAfter(Entity<ChaplainComponent> ent, ref ChaplainMakeBelieverDoAfter args)
    {
        if (args.Handled || args.Args.Target is not { } target)
            return;

        args.Handled = true;

        if (args.Cancelled)
        {
            ChangeFaith((ent.Owner, ent.Comp), ent.Comp.BelieverCost);
            return;
        }

        if (ent.Comp.Followers.Count >= ent.Comp.MaxFollowers)
        {
            _popup.PopupClient(Loc.GetString("chaplain-believer-too-much"), ent, ent);
            return;
        }

        OfferReligion(ent, target);
    }

    protected virtual void OfferReligion(Entity<ChaplainComponent> ent, EntityUid target)
    {
    }

    public void MakeFollower(Entity<ChaplainComponent> ent, EntityUid target)
    {
        if (HasComp<ChaplainComponent>(target) || HasComp<ChaplainFollowerComponent>(target) || HasComp<MindShieldComponent>(target))
            return;

        var follower = EnsureComp<ChaplainFollowerComponent>(target);
        follower.Chaplain = ent.Owner;
        Dirty(target, follower);

        ent.Comp.Followers.Add(target);
        Dirty(ent);

        _popup.PopupEntity(Loc.GetString("chaplain-believer-success-self", ("target", Identity.Entity(target, EntityManager))), ent, ent);
        _popup.PopupEntity(Loc.GetString("chaplain-believer-success-target", ("issuer", Identity.Entity(ent, EntityManager))), target, target);
    }

    #endregion

    #region Holy shield

    private void OnHolyShield(Entity<ChaplainComponent> ent, ref ChaplainHolyShieldActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseFaith(ent, ent.Comp.ShieldCost))
            return;

        args.Handled = true;
        GrantHolyShield(ent, ent.Comp.ShieldDuration);
        _audio.PlayPredicted(ent.Comp.ShieldSound, ent, ent);
        _popup.PopupClient(Loc.GetString("chaplain-holy-shield-start"), ent, ent);
    }

    private void OnExorcism(Entity<ChaplainComponent> ent, ref ChaplainExorcismActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseFaith(ent, ent.Comp.ExorcismCost))
            return;

        args.Handled = true;
        var target = args.Target;

        _audio.PlayPredicted(ent.Comp.ExorcismSound, target, ent);
        _popup.PopupClient(Loc.GetString("chaplain-exorcism-self", ("target", Identity.Entity(target, EntityManager))), ent, ent);

        if (!TryComp<HolyAffectedComponent>(target, out var holy))
            return;

        _damageable.TryChangeDamage(target, ent.Comp.ExorcismDamage, origin: ent);
        _movementMod.TryUpdateMovementSpeedModDuration(target, ExorcismSlowdownEffect, ent.Comp.ExorcismSlowdown, ent.Comp.ExorcismSlowdownMultiplier);

        if (holy.Suppressible)
            AddSuppression((target, holy), ent.Comp.ExorcismSuppression);

        _popup.PopupEntity(Loc.GetString("chaplain-exorcism-target"), target, target, PopupType.LargeCaution);
    }

    private void OnBlessing(Entity<ChaplainComponent> ent, ref ChaplainBlessingActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryUseFaith(ent, ent.Comp.BlessingCost))
            return;

        args.Handled = true;
        var target = args.Target;

        _audio.PlayPredicted(ent.Comp.BlessingSound, target, ent);
        _popup.PopupClient(Loc.GetString("chaplain-blessing-self", ("target", Identity.Entity(target, EntityManager))), ent, ent);
        _popup.PopupEntity(Loc.GetString("chaplain-blessing-target", ("issuer", Identity.Entity(ent, EntityManager))), target, target);

        if (!HasComp<HolyAffectedComponent>(target))
            _groupHeal.Heal(target, null, ent.Comp.BlessingHeal, origin: ent);
    }

    public void GrantHolyShield(EntityUid uid, TimeSpan duration)
    {
        var shield = EnsureComp<HolyShieldComponent>(uid);
        var end = _timing.CurTime + duration;
        if (end > shield.EndTime)
            shield.EndTime = end;

        Dirty(uid, shield);
    }

    private void UpdateShields(TimeSpan now)
    {
        var query = EntityQueryEnumerator<HolyShieldComponent>();
        while (query.MoveNext(out var uid, out var shield))
        {
            if (now < shield.EndTime)
                continue;

            RemCompDeferred<HolyShieldComponent>(uid);
            _popup.PopupEntity(Loc.GetString("chaplain-holy-shield-end"), uid, uid);
        }
    }

    #endregion

    #region Holy touch

    private void OnHolyTouch(Entity<ChaplainComponent> ent, ref ChaplainHolyTouchActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var target = args.Target;

        if (ent.Comp.Faith < ent.Comp.HolyWaterCost)
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

        ChangeFaith((ent.Owner, ent.Comp), -ent.Comp.HolyWaterCost);
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

        ChangeFaith((ent.Owner, ent.Comp), -ent.Comp.HolyWaterCost);
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
}
