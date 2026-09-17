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

    private readonly List<EntProtoId> _order = new();
    private readonly Dictionary<EntProtoId, int> _places = new();
    private readonly HashSet<EntProtoId> _removed = new();

    private EntityUid? _cachedFor;
    private EntityUid? _syncedFor;

    private bool _pendingSend;

    public IReadOnlyDictionary<EntProtoId, int> Order
    {
        get
        {
            EnsureCache();
            return _places;
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
        _pendingSend = false;
        _cachedFor = null;
        _syncedFor = HasComp<ADTActionOrderComponent>(ev.Entity) ? ev.Entity : null;

        RebuildCache(ev.Entity);

        _ui.GetUIController<ActionUIController>().ReloadActionOrder();
    }

    private void OnHandleState(Entity<ADTActionOrderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var wasSynced = _syncedFor == ent.Owner;

        _syncedFor = ent.Owner;

        if (_player.LocalEntity != ent.Owner)
            return;

        if (wasSynced && _cachedFor == ent.Owner)
        {
            Flush();
            return;
        }

        RebuildCache(ent.Owner);
        _ui.GetUIController<ActionUIController>().ReloadActionOrder();

        Flush();
    }

    public void SetRemoved(EntProtoId action, bool removed)
    {
        if (_player.LocalEntity == null)
            return;

        EnsureCache();

        if (removed)
        {
            if (!_removed.Add(action))
                return;
        }
        else if (!_removed.Remove(action))
        {
            return;
        }

        Send();
    }

    public void Store(List<EntityUid?> actions)
    {
        if (_player.LocalEntity == null)
            return;

        EnsureCache();

        var present = new List<EntProtoId>();
        foreach (var action in actions)
        {
            if (action is not { } actionId)
                continue;

            if (GetActionProto(actionId) is not { } proto)
                continue;

            if (present.Contains(proto))
                continue;

            present.Add(proto);
        }

        foreach (var proto in present)
        {
            if (_places.ContainsKey(proto) || _order.Count >= MaxOrderEntries)
                continue;

            _order.Add(proto);
            _places[proto] = _order.Count - 1;
        }

        present.RemoveAll(proto => !_places.ContainsKey(proto));

        var slots = new List<int>();
        for (var i = 0; i < _order.Count; i++)
        {
            if (present.Contains(_order[i]))
                slots.Add(i);
        }

        var count = Math.Min(slots.Count, present.Count);
        for (var i = 0; i < count; i++)
        {
            _order[slots[i]] = present[i];
        }

        RebuildPlaces();
        Send();
    }

    private void Send()
    {
        if (_player.LocalEntity is not { } uid)
            return;

        if (_syncedFor != uid)
        {
            _pendingSend = true;
            return;
        }

        _pendingSend = false;

        if (TryComp(uid, out ADTActionOrderComponent? order) && Matches(order))
            return;

        RaiseNetworkEvent(new ADTActionOrderChangeEvent(_order.ToList(), _removed.ToList()));
    }

    private void Flush()
    {
        if (_pendingSend)
            Send();
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
        _places.Clear();
        _removed.Clear();
        _cachedFor = player;

        if (player is not { } uid || !TryComp(uid, out ADTActionOrderComponent? order))
            return;

        if (!order.Order.IsDefaultOrEmpty)
        {
            foreach (var action in order.Order)
            {
                if (_places.ContainsKey(action))
                    continue;

                _order.Add(action);
                _places[action] = _order.Count - 1;
            }
        }

        if (order.Removed.IsDefaultOrEmpty)
            return;

        foreach (var action in order.Removed)
        {
            _removed.Add(action);
        }
    }

    private void RebuildPlaces()
    {
        _places.Clear();
        for (var i = 0; i < _order.Count; i++)
        {
            _places[_order[i]] = i;
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
            if (order.Order[i] != _order[i])
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
