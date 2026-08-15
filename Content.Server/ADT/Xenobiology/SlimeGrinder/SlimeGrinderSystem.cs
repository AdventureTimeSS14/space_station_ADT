using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Events;
using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.ADT.Xenobiology.Systems;
using Content.Shared.ADT.Xenobiology.SlimeGrinder;
using Content.Server.Power.Components;
using Content.Shared.Audio;
using Content.Shared.Climbing.Events;
using Content.Shared.Construction.Components;
using Content.Shared.Jittering;
using Content.Shared.Medical;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Power;
using Content.Shared.Throwing;
using Robust.Server.Containers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Xenobiology.SlimeGrinder;

public sealed partial class SlimeGrinderSystem : EntitySystem
{
    [Dependency] private readonly XenobiologySystem _xenobio = default!;
    [Dependency] private readonly SharedJitteringSystem _jitteringSystem = default!;
    [Dependency] private readonly SharedAudioSystem _sharedAudioSystem = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSoundSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeLocalEvent<SlimeGrinderComponent, MapInitEvent>(OnGrinderMapInit);
        SubscribeLocalEvent<ActiveSlimeGrinderComponent, ComponentInit>(OnActiveInit);
        SubscribeLocalEvent<ActiveSlimeGrinderComponent, ComponentRemove>(OnActiveShutdown);
        SubscribeLocalEvent<ActiveSlimeGrinderComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
        SubscribeLocalEvent<SlimeGrinderComponent, ClimbedOnEvent>(OnClimbedOn);
        SubscribeLocalEvent<SlimeGrinderComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SlimeGrinderComponent, ReclaimerDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<SlimeGrinderComponent, RefreshPartsEvent>(OnRefreshParts);
        SubscribeLocalEvent<SlimeGrinderComponent, UpgradeExamineEvent>(OnUpgradeExamine);
    }

    private void OnRefreshParts(EntityUid uid, SlimeGrinderComponent component, RefreshPartsEvent args)
    {
        var speed = args.GetStatMultiplier(MachineStat.Speed);
        component.ExtractMultiplier = speed;
        component.WorkTimeMultiplier = 1f / speed;
    }

    private static void OnUpgradeExamine(EntityUid uid, SlimeGrinderComponent component, UpgradeExamineEvent args)
    {
        args.AddPercentageUpgrade("machine-upgrade-slime-extract-multiplier", component.ExtractMultiplier, benefit: true);
        args.AddPercentageUpgrade("machine-upgrade-process-speed", 1f / component.WorkTimeMultiplier, benefit: true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        AutoFeed();

        var query = EntityQueryEnumerator<ActiveSlimeGrinderComponent, SlimeGrinderComponent>();
        while (query.MoveNext(out var uid, out _, out var grinder))
        {
            grinder.ProcessingTimer = Math.Clamp(grinder.ProcessingTimer - frameTime, 0, grinder.ProcessingTimer);

            if (grinder.ProcessingTimer > 0)
                continue;

            foreach (var (protoId, count) in grinder.YieldQueue)
            {
                for (var i = 0; i < count; i++)
                    SpawnNextToOrDrop(protoId, uid);
            }
            grinder.YieldQueue.Clear();

            if (HasComp<ActiveSlimeGrinderComponent>(uid))
                RemCompDeferred<ActiveSlimeGrinderComponent>(uid);
        }
    }

    #region Active Grinding

    private void OnActiveInit(Entity<ActiveSlimeGrinderComponent> activeGrinder, ref ComponentInit args)
    {
        if (!TryComp<SlimeGrinderComponent>(activeGrinder, out var grinder))
            return;

        _jitteringSystem.AddJitter(activeGrinder, -10, 100);
        _sharedAudioSystem.PlayPvs(grinder.GrindSound, activeGrinder);
        _ambientSoundSystem.SetAmbience(activeGrinder, true);
    }

    private void OnActiveShutdown(Entity<ActiveSlimeGrinderComponent> activeGrinder, ref ComponentRemove args)
    {
        RemComp<JitteringComponent>(activeGrinder);
        _ambientSoundSystem.SetAmbience(activeGrinder, false);
    }

    private void OnUnanchorAttempt(Entity<ActiveSlimeGrinderComponent> activeGrinder, ref UnanchorAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnPowerChanged(Entity<SlimeGrinderComponent> grinder, ref PowerChangedEvent args)
    {
        if (args.Powered && grinder.Comp.ProcessingTimer > 0)
            EnsureComp<ActiveSlimeGrinderComponent>(grinder);
        else
            RemCompDeferred<ActiveSlimeGrinderComponent>(grinder);
    }

    #endregion

    private void OnGrinderMapInit(Entity<SlimeGrinderComponent> grinder, ref MapInitEvent args)
    {
        grinder.Comp.NextScan = _timing.CurTime;
    }

    private void AutoFeed()
    {
        var currentTime = _timing.CurTime;
        var query = EntityQueryEnumerator<SlimeGrinderComponent, TransformComponent>();
        var candidates = new HashSet<Entity<SlimeComponent>>();

        while (query.MoveNext(out var uid, out var grinder, out var xform))
        {
            if (grinder.AutoFeedRange <= 0 || grinder.NextScan > currentTime)
                continue;

            grinder.NextScan += grinder.ScanInterval;

            if (!CanOperate(uid))
                continue;

            candidates.Clear();
            _lookup.GetEntitiesInRange(xform.Coordinates, grinder.AutoFeedRange, candidates, LookupFlags.Dynamic | LookupFlags.Sundries);

            foreach (var candidate in candidates)
            {
                var candidateUid = candidate.Owner;

                if (!IsValidSlimeCorpse(candidateUid))
                    continue;

                if (!_physicsQuery.TryGetComponent(candidateUid, out var physics) || physics.BodyStatus != BodyStatus.OnGround)
                    continue;

                QueueProcess(candidateUid, (uid, grinder), physics, candidate.Comp);
            }
        }
    }

    private void OnClimbedOn(Entity<SlimeGrinderComponent> grinder, ref ClimbedOnEvent args)
    {
        if (CanGrind(grinder, args.Climber))
            QueueProcess(args.Climber, grinder);
    }

    private void OnDoAfter(Entity<SlimeGrinderComponent> grinder, ref ReclaimerDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Used is not { } toProcess)
            return;

        QueueProcess(toProcess, grinder);
        args.Handled = true;
    }

    private void QueueProcess(EntityUid toProcess, Entity<SlimeGrinderComponent> grinder, PhysicsComponent? physics = null, SlimeComponent? slime = null)
    {
        if (!Resolve(toProcess, ref physics, ref slime))
            return;

        EnsureComp<ActiveSlimeGrinderComponent>(grinder);
        grinder.Comp.ProcessingTimer += physics.FixturesMass * grinder.Comp.ProcessingTimePerUnitMass * grinder.Comp.WorkTimeMultiplier;

        var extractProto = _xenobio.GetProducedExtract((toProcess, slime));
        var extractQuantity = (int)MathF.Round(slime.ExtractsProduced * grinder.Comp.ExtractMultiplier);

        if (!grinder.Comp.YieldQueue.ContainsKey(extractProto))
            grinder.Comp.YieldQueue.Add(extractProto, extractQuantity);
        else
            grinder.Comp.YieldQueue[extractProto] += extractQuantity;

        foreach (var ent in _container.EmptyContainer(slime.Stomach))
        {
            _container.TryRemoveFromContainer(ent, true);
            _throwing.TryThrow(ent, _robustRandom.NextVector2() * 5);
        }
        QueueDel(toProcess);
    }

    private bool CanGrind(EntityUid grinder, EntityUid dragged)
    {
        return CanOperate(grinder) && IsValidSlimeCorpse(dragged);
    }

    private bool CanOperate(EntityUid grinder)
    {
        return Transform(grinder).Anchored
            && (!TryComp<ApcPowerReceiverComponent>(grinder, out var power) || power.Powered);
    }

    private bool IsValidSlimeCorpse(EntityUid candidate)
    {
        return HasComp<SlimeComponent>(candidate)
            && (!TryComp<MobStateComponent>(candidate, out var mobState) || mobState.CurrentState == MobState.Dead);
    }
}
