using Content.Shared.Buckle.Components;
using Content.Shared.GameTicking;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Camera;

public sealed class ShakeOnStairsSystem : EntitySystem
{
    [Dependency] private readonly ScreenshakeSystem _shake = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private readonly Dictionary<EntityUid, MapCoordinates> _lastShakeCoords = [];
    private static readonly ProtoId<TagPrototype> StairTag = new("Stairs");
    private static readonly string ShakeKey = "stairShake";

    public override void Initialize()
    {
        base.Initialize();

        _xform.OnGlobalMoveEvent += OnMoveEvent;
        SubscribeLocalEvent<EntityTerminatingEvent>(OnTerminating);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnCleanup);
    }

    private void OnMoveEvent(ref MoveEvent ev)
    {
        // Only strapped movers (wheelchair etc.) matter — skip the global buckle scan.
        if (!TryComp<StrapComponent>(ev.Sender, out var strap) || strap.BuckledEntities.Count == 0)
            return;

        var nearStairs = false;
        foreach (var nearby in _lookup.GetEntitiesInRange(ev.Sender, 0.5f))
        {
            if (!_tag.HasTag(nearby, StairTag))
                continue;

            nearStairs = true;
            break;
        }

        if (!nearStairs)
            return;

        foreach (var uid in strap.BuckledEntities)
        {
            if (_shake.IsOnCooldown(uid, ShakeKey))
                continue;

            var currentCoords = _xform.GetMapCoordinates(uid);
            if (_lastShakeCoords.TryGetValue(uid, out var coords) && currentCoords.InRange(coords, 0.22f))
                continue;

            _lastShakeCoords[uid] = currentCoords;
            var translation = new ScreenshakeParameters
            {
                Trauma = 0.4f,
                DecayRate = 1.8f,
                Frequency = 0.02f,
            };
            var rotation = new ScreenshakeParameters
            {
                Trauma = 0.14f,
                DecayRate = 1.2f,
                Frequency = 0.013f,
            };
            _shake.Screenshake(uid, translation, rotation, ShakeKey, 0.05f);
        }
    }

    private void OnTerminating(ref EntityTerminatingEvent ev) => _lastShakeCoords.Remove(ev.Entity);

    private void OnCleanup(RoundRestartCleanupEvent ev) => _lastShakeCoords.Clear();
}
