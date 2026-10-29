using Content.Shared.ADT.Particles;
using Content.Shared.DoAfter;
using Content.Shared.Tools.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Particles;

public sealed class WelderParticleMarkerSystem : EntitySystem
{
    private static readonly EntProtoId MarkerProto = "OverlayWeldSparksEffect";

    [Dependency] private SharedTransformSystem _transform = default!;

    private readonly Dictionary<DoAfterId, EntityUid> _markers = new();
    private readonly List<DoAfterId> _toRemove = new();
    private readonly HashSet<DoAfterId> _seen = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DoAfterComponent, ComponentShutdown>(OnDoAfterShutdown);
    }

    public override void Update(float frameTime)
    {
        _seen.Clear();

        var enumerator = EntityQueryEnumerator<DoAfterComponent, ActiveDoAfterComponent>();
        while (enumerator.MoveNext(out var uid, out var comp, out _))
        {
            foreach (var doAfter in comp.DoAfters.Values)
            {
                if (doAfter.Completed || doAfter.Cancelled)
                    continue;

                if (doAfter.Args.Event is not SharedToolSystem.ToolDoAfterEvent { Fuel: > 0f })
                    continue;

                if (doAfter.Args.Target is not { } target)
                    continue;

                if (TerminatingOrDeleted(target))
                    continue;

                EnsureMarker(doAfter.Id, target);
                _seen.Add(doAfter.Id);
            }
        }

        _toRemove.Clear();
        foreach (var id in _markers.Keys)
        {
            if (!_seen.Contains(id))
                _toRemove.Add(id);
        }

        foreach (var id in _toRemove)
        {
            if (_markers.Remove(id, out var marker) && !TerminatingOrDeleted(marker))
                QueueDel(marker);
        }
    }

    private void EnsureMarker(DoAfterId id, EntityUid target)
    {
        if (_markers.TryGetValue(id, out var marker))
        {
            if (!TerminatingOrDeleted(marker))
                return;

            _markers.Remove(id);
        }

        marker = SpawnAttachedTo(MarkerProto, Transform(target).Coordinates);
        _transform.SetParent(marker, target);
        _markers[id] = marker;
    }

    private void OnDoAfterShutdown(Entity<DoAfterComponent> ent, ref ComponentShutdown args)
    {
        _toRemove.Clear();
        foreach (var id in _markers.Keys)
        {
            if (id.Uid != ent.Owner)
                continue;

            _toRemove.Add(id);
        }

        foreach (var id in _toRemove)
        {
            if (_markers.Remove(id, out var marker) && !TerminatingOrDeleted(marker))
                QueueDel(marker);
        }
    }
}
