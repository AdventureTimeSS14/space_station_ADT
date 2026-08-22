using System.Linq;
using Content.Client.UserInterface.Systems.Actions;
using Content.Shared.ADT.Actions;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Actions;

public sealed class ADTActionOrderSystem : EntitySystem
{
    private const int MaxOrderEntries = 64;

    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private readonly Dictionary<EntProtoId, int> _order = new();
    private readonly HashSet<EntProtoId> _removed = new();
    private EntityUid? _cachedFor;
    private EntityUid? _syncedFor;

    public IReadOnlyDictionary<EntProtoId, int> Order
    {
        get
        {
            EnsureCache();
            return _order;
        }
    }

    public IReadOnlySet<EntProtoId> Removed
    {
        get
        {
            EnsureCache();
            return _removed;
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTActionOrderComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnLocalPlayerAttached);
    }

    private void OnLocalPlayerAttached(LocalPlayerAttachedEvent ev)
    {
        _cachedFor = null;
        RebuildCache(ev.Entity);
    }

    private void OnHandleState(Entity<ADTActionOrderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var wasSynced = _syncedFor == ent.Owner;
        
        _syncedFor = ent.Owner;

        if (_player.LocalEntity != ent.Owner)
            return;

        if (wasSynced && _cachedFor == ent.Owner)
            return;

        RebuildCache(ent.Owner);
        _ui.GetUIController<ActionUIController>().ReloadActionOrder();
    }

    public void SetRemoved(EntProtoId action, bool removed)
    {
        if (!CanStore(out _))
            return;

        EnsureCache();

        if (removed)
        {
            _removed.Add(action);
            return;
        }

        _removed.Remove(action);
    }

    public void Store(List<EntityUid?> actions)
    {
        if (!CanStore(out _))
            return;

        EnsureCache();

        var order = new List<EntProtoId>();
        foreach (var action in actions)
        {
            if (action is not { } actionId)
                continue;

            if (GetActionProto(actionId) is not { } proto)
                continue;

            if (order.Contains(proto))
                continue;

            order.Add(proto);
        }

        foreach (var (proto, place) in _order.OrderBy(entry => entry.Value))
        {
            if (order.Count >= MaxOrderEntries)
                break;

            if (order.Contains(proto))
                continue;

            order.Insert(Math.Min(place, order.Count), proto);
        }

        _order.Clear();
        for (var i = 0; i < order.Count; i++)
        {
            _order[order[i]] = i;
        }

        var removed = _removed.Count > MaxOrderEntries
            ? _removed.Take(MaxOrderEntries).ToList()
            : _removed.ToList();

        RaiseNetworkEvent(new ADTActionOrderChangeEvent(order, removed));
    }

    private bool CanStore(out EntityUid player)
    {
        player = default;

        if (_player.LocalEntity is not { } uid || _syncedFor != uid)
            return false;

        if (!HasComp<ADTActionOrderComponent>(uid))
            return false;

        player = uid;
        return true;
    }

    private void EnsureCache()
    {
        if (_cachedFor == _player.LocalEntity)
            return;

        RebuildCache(_player.LocalEntity);
    }

    private void RebuildCache(EntityUid? player)
    {
        _order.Clear();
        _removed.Clear();
        _cachedFor = player;

        if (player is not { } uid || !TryComp(uid, out ADTActionOrderComponent? order))
            return;

        if (!order.Order.IsDefaultOrEmpty)
        {
            for (var i = 0; i < order.Order.Length; i++)
            {
                _order[order.Order[i]] = i;
            }
        }

        if (!order.Removed.IsDefaultOrEmpty)
        {
            foreach (var action in order.Removed)
            {
                _removed.Add(action);
            }
        }
    }

    private bool Matches(ADTActionOrderComponent order)
    {
        var orderCount = order.Order.IsDefault ? 0 : order.Order.Length;
        var removedCount = order.Removed.IsDefault ? 0 : order.Removed.Length;

        if (orderCount != _order.Count || removedCount != _removed.Count)
            return false;

        for (var i = 0; i < orderCount; i++)
        {
            if (!_order.TryGetValue(order.Order[i], out var place) || place != i)
                return false;
        }

        for (var i = 0; i < removedCount; i++)
        {
            if (!_removed.Contains(order.Removed[i]))
                return false;
        }

        return true;
    }

    private EntProtoId? GetActionProto(EntityUid action)
    {
        if (!TryComp(action, out MetaDataComponent? metaData) || metaData.EntityPrototype is not { } proto)
            return null;

        return proto.ID;
    }
}
