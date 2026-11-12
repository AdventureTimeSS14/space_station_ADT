using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.Prototypes;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Nutrition.EntitySystems;

/// <summary>
/// This abstract system provides a convenient interface for implementing effects which react to changes in
/// <see cref="Satiation"/> thresholds.
/// </summary>
/// <example>
/// <see cref="SatiationDamageSystem"/> uses this to maintain a damage descriptor which changes depending on the entity's
/// satiation values. When the satiation values change, the damage descriptor is automatically updated by this system.
/// </example>
/// <typeparam name="TComp">The type of component this system interacts with</typeparam>
/// <typeparam name="T">
/// The type of value this system maintains. It must be contained within a <see cref="SatiationThresholds{T}"/>
/// <see cref="GetThresholds">accessible via <typeparamref name="TComp">TComp</typeparamref></see>.
/// </typeparam>
/// <remarks>Note that this <b>is not</b> related to <see cref="EntityEffects"/></remarks>
public abstract partial class BaseSatiationEffectSystem<TComp, T> : EntitySystem where TComp : Component
{
    [Dependency] private IGameTiming _timing = default!;

    [Dependency] private SatiationSystem _satiation = default!;

    [Dependency] private EntityQuery<SatiationComponent> _satiationQuery;

    // ADT-Tweak-Start
    [Dependency] private INetManager _net = default!;

    private static readonly TimeSpan PausedRetryDelay = TimeSpan.FromSeconds(1);

    private readonly SortedSet<(TimeSpan Due, EntityUid Uid)> _dueQueue = new();
    private readonly Dictionary<EntityUid, TimeSpan> _scheduled = new();
    private readonly List<EntityUid> _due = new();
    // ADT-Tweak-End

    /// <summary>
    /// How to access <see cref="SatiationThresholds{T}"/> via a <typeparamref name="TComp"/>.
    /// </summary>
    protected abstract Dictionary<ProtoId<SatiationTypePrototype>, SatiationThresholds<T>> GetThresholds(TComp comp);

    /// <summary>
    /// The default <typeparamref name="T"/> value to set our maintained value to in the case that our current satiation
    /// is outside of <see cref="GetThresholds"/>.
    /// </summary>
    protected abstract T DefaultValue();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TComp, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TComp, SatiationUpdateEvent>(OnSatiationUpdate);
        // ADT-Tweak-Start
        SubscribeLocalEvent<TComp, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<TComp, ComponentShutdown>(OnShutdown);
        // ADT-Tweak-End
    }

    /// <inheritdoc/>
    // Updates maintained values when reaching the projected threshold time.
    public override void Update(float frameTime)
    {
        // ADT-Tweak-Start
        if (_net.IsServer)
        {
            UpdateDueQueue();
            return;
        }
        // ADT-Tweak-End

        var query = EntityQueryEnumerator<TComp, SatiationComponent>();
        while (query.MoveNext(out var ent, out var comp, out var satiation))
        {
            foreach (var (type, thresholds) in GetThresholds(comp))
            {
                if (thresholds.ProjectedThresholdChangeTime == null ||
                    _timing.CurTime < thresholds.ProjectedThresholdChangeTime)
                    continue;

                UpdateSatiation((ent, comp), satiation, type);
            }
        }
    }

    // ADT-Tweak-Start
    private void OnStartup(Entity<TComp> entity, ref ComponentStartup args)
    {
        if (!_net.IsServer)
            return;

        Reschedule(entity);
    }

    private void OnShutdown(Entity<TComp> entity, ref ComponentShutdown args)
    {
        if (_net.IsServer)
            SetDue(entity.Owner, null);
    }

    private void Reschedule(Entity<TComp> entity)
    {
        TimeSpan? min = null;
        foreach (var thresholds in GetThresholds(entity.Comp).Values)
        {
            if (thresholds.ProjectedThresholdChangeTime is { } due && (min == null || due < min))
                min = due;
        }

        SetDue(entity.Owner, min);
    }

    private void SetDue(EntityUid uid, TimeSpan? due)
    {
        if (_scheduled.Remove(uid, out var old))
            _dueQueue.Remove((old, uid));

        if (due is not { } next)
            return;

        _scheduled[uid] = next;
        _dueQueue.Add((next, uid));
    }

    private void UpdateDueQueue()
    {
        var now = _timing.CurTime;
        _due.Clear();
        while (_dueQueue.Count > 0 && _dueQueue.Min.Due <= now)
        {
            var first = _dueQueue.Min;
            _dueQueue.Remove(first);
            _scheduled.Remove(first.Uid);
            _due.Add(first.Uid);
        }

        foreach (var uid in _due)
        {
            if (!TryComp<TComp>(uid, out var comp) || !_satiationQuery.TryComp(uid, out var satiation))
                continue;

            if (Paused(uid))
            {
                SetDue(uid, now + PausedRetryDelay);
                continue;
            }

            foreach (var (type, thresholds) in GetThresholds(comp))
            {
                if (thresholds.ProjectedThresholdChangeTime is not { } next || now < next)
                    continue;

                UpdateSatiation((uid, comp), satiation, type);
            }

            Reschedule((uid, comp));
        }
    }
    // ADT-Tweak-End

    [MustCallBase]
    protected virtual void OnMapInit(Entity<TComp> entity, ref MapInitEvent args)
    {
        // Make sure we have a satiation component. Realistically, this just exists to cause test failures if an entity
        // with `TComp` doesn't have a `SatiationComponent`.
        var comp = EnsureComp<SatiationComponent>(entity);
        foreach (var type in GetThresholds(entity.Comp).Keys)
        {
            UpdateSatiation(entity, comp, type);
        }
    }

    // If a satiation value is changed directly, we react and update our maintained value.
    [MustCallBase]
    protected void OnSatiationUpdate(Entity<TComp> entity, ref SatiationUpdateEvent args)
    {
        if (!_satiationQuery.TryComp(entity, out var comp))
            return;

        UpdateSatiation(entity, comp, args.Type);
    }

    /// <summary>
    /// Updates the maintained <typeparamref name="T"/> based on the entity's satiation.
    /// </summary>
    private void UpdateSatiation(Entity<TComp> entity, SatiationComponent comp, ProtoId<SatiationTypePrototype> type)
    {
        if (!GetThresholds(entity.Comp).TryGetValue(type, out var thresholds))
            return;
        var satiation = new Entity<SatiationComponent>(entity, comp);

        var gotThreshold = _satiation.TryGetValueByThreshold(
            satiation,
            type,
            thresholds.Thresholds,
            out var result,
            out var nextHigherThreshold,
            out var nextLowerThreshold
        );

        thresholds.Current = gotThreshold ? result ?? DefaultValue() : DefaultValue();
        thresholds.ProjectedThresholdChangeTime = _satiation.GetTimeToBound(
            satiation,
            type,
            nextHigherThreshold,
            nextLowerThreshold
        );

        if (_net.IsServer) // ADT-Tweak
            Reschedule(entity); // ADT-Tweak

        Dirty(entity);

        AfterSatiationUpdate(entity);
    }

    /// <summary>
    /// This function is called after <see cref="UpdateSatiation"/> completes its work maintaining <typeparamref name="T"/>.
    /// </summary>
    protected virtual void AfterSatiationUpdate(Entity<TComp> entity) { }
}
