using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Content.ADT.Shared.Chaplain.Sacrifice;

namespace Content.Server.Altar.Systems;

public sealed class AltarLimitSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private readonly Dictionary<EntityUid, int> _gridCounts = new();

    [DataField("disableSound")]
    public SoundSpecifier DisableSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AltarControlComponent, ComponentStartup>(OnAltarStartup);
        SubscribeLocalEvent<AltarControlComponent, ComponentRemove>(OnAltarRemove);
        SubscribeLocalEvent<AltarControlComponent, EntityTerminatingEvent>(OnAltarTerminating);

        InitializeCounts();
    }

    private void InitializeCounts()
    {
        var query = EntityQueryEnumerator<AltarControlComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var altar, out var transform))
        {
            var gridUid = transform.GridUid;
            if (gridUid != null)
            {
                if (!_gridCounts.ContainsKey(gridUid.Value))
                    _gridCounts[gridUid.Value] = 0;

                _gridCounts[gridUid.Value]++;
            }
        }
    }

    private void OnAltarStartup(EntityUid uid, AltarControlComponent component, ComponentStartup args)
    {
        if (!TryComp<TransformComponent>(uid, out var transform))
            return;

        var gridUid = transform.GridUid;
        if (gridUid == null)
            return;

        if (!_gridCounts.ContainsKey(gridUid.Value))
            _gridCounts[gridUid.Value] = 0;

        _gridCounts[gridUid.Value]++;

        if (_gridCounts[gridUid.Value] > 3)
        {
            var coordinates = transform.Coordinates;

            RemComp<SacrificeComponent>(uid);

            _popup.PopupCoordinates(
                Loc.GetString("altar-limit-exceeded"),
                coordinates,
                PopupType.MediumCaution
            );

            _audio.PlayPvs(DisableSound, coordinates);
        }
    }

    private void OnAltarRemove(EntityUid uid, AltarControlComponent component, ComponentRemove args)
    {
        UpdateGridCount(uid);
    }

    private void OnAltarTerminating(EntityUid uid, AltarControlComponent component, EntityTerminatingEvent args)
    {
        UpdateGridCount(uid);
    }

    private void UpdateGridCount(EntityUid altarUid)
    {
        if (!TryComp<TransformComponent>(altarUid, out var transform))
            return;

        var gridUid = transform.GridUid;
        if (gridUid == null)
            return;

        if (_gridCounts.ContainsKey(gridUid.Value))
        {
            _gridCounts[gridUid.Value] = Math.Max(0, _gridCounts[gridUid.Value] - 1);
        }
    }

    public int GetAltarCountOnGrid(EntityUid gridUid)
    {
        return _gridCounts.TryGetValue(gridUid, out var count) ? count : 0;
    }
}