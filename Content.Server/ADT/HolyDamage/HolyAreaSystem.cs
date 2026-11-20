using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Shared.ADT.Areas;
using Content.Shared.ADT.HolyDamage;
using Content.Shared.ADT.Vampire.Components;
using Content.Shared.Atmos.Components;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.ADT.HolyDamage;

public sealed class HolyAreaSystem : EntitySystem
{
    [Dependency] private AreaSystem _areas = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private FlammableSystem _flammable = default!;
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private static readonly ProtoId<EmotePrototype> Scream = "Scream";
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(1);

    private readonly Dictionary<EntityUid, TimeSpan> _nextBurn = new();
    private TimeSpan _nextCheck;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (now < _nextCheck)
            return;

        _nextCheck = now + CheckInterval;

        foreach (var uid in _nextBurn.Keys.Where(k => TerminatingOrDeleted(k)).ToList())
        {
            _nextBurn.Remove(uid);
        }

        var query = EntityQueryEnumerator<VampireComponent, FlammableComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var flammable, out var xform))
        {
            if (HasComp<SupremeVampireComponent>(uid) || !TryGetHolyArea(xform.Coordinates, out var holy))
            {
                _nextBurn.Remove(uid);
                continue;
            }

            if (_nextBurn.TryGetValue(uid, out var next) && now < next)
                continue;

            _nextBurn[uid] = now + holy.Interval;

            flammable.FireStacks += holy.FireStacks;
            _flammable.Ignite(uid, uid, flammable);

            _chat.TryEmoteWithoutChat(uid, ProtoMan.Index(Scream), true);
            _popup.PopupEntity(Loc.GetString("vampire-holy-point"), uid, uid, PopupType.LargeCaution);
        }
    }

    public bool TryGetHolyArea(EntityCoordinates coordinates, [NotNullWhen(true)] out HolyAreaComponent? holy)
    {
        holy = null;

        if (_areas.GetAreaPrototypeId(coordinates) is not { } areaId ||
            !ProtoMan.TryIndex((EntProtoId) areaId, out var area))
        {
            return false;
        }

        return area.TryGetComponent(out holy, _factory);
    }
}
