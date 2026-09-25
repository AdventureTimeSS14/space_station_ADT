using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Audio;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.ADT.Lavaland;
using Content.Shared.ADT.Mining.Drill;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Power;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Mining;
using Content.Shared.Popups;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Content.Shared.Wires;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Mining.Drill;

public sealed class ADTDrillSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly PowerStateSystem _powerState = default!;
    [Dependency] private readonly AmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    internal static readonly Direction[] CardinalDirs =
        { Direction.North, Direction.South, Direction.East, Direction.West };

    public const int MaxFillLevel = 3;

    public override void Initialize()
    {
        SubscribeLocalEvent<ADTDrillComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ADTDrillComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<ADTDrillComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<ADTDrillComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ADTDrillComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<ADTDrillComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        SubscribeLocalEvent<ADTDrillBraceComponent, ComponentInit>(OnBraceInit);
        SubscribeLocalEvent<ADTDrillBraceComponent, AnchorStateChangedEvent>(OnBraceAnchorChanged);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTDrillComponent>();
        while (query.MoveNext(out var uid, out var drill))
        {
            if (!drill.Active || drill.Error)
                continue;

            if (drill.NextTick > now)
                continue;

            drill.NextTick = now + drill.TickInterval;
            TickDrill(uid, drill);
        }
    }

    private void OnPowerChanged(EntityUid uid, ADTDrillComponent drill, ref PowerChangedEvent args)
    {
        if (drill.Active && !args.Powered)
            SystemError(uid, drill, Loc.GetString("drill-error-power"));
    }

    private void OnAnchorChanged(EntityUid uid, ADTDrillComponent drill, ref AnchorStateChangedEvent args)
    {
        if (drill.Active && !args.Anchored)
            SystemError(uid, drill, Loc.GetString("drill-error-bracing"));

        RefreshAdjacentBraces(uid);
    }

    private void OnBraceInit(EntityUid uid, ADTDrillBraceComponent component, ComponentInit args)
    {
        UpdateBraceConnection(uid);
    }

    private void OnBraceAnchorChanged(EntityUid uid, ADTDrillBraceComponent component, ref AnchorStateChangedEvent args)
    {
        UpdateBraceConnection(uid);
    }

    private void RefreshAdjacentBraces(EntityUid uid)
    {
        ForEachAdjacentAnchored(Transform(uid), ent =>
        {
            if (HasComp<ADTDrillBraceComponent>(ent))
                UpdateBraceConnection(ent);
        });
    }

    public void UpdateBraceConnection(EntityUid braceUid)
    {
        var xform = Transform(braceUid);

        RefreshAdjacentDrills(braceUid, xform, out var connectedUid);

        if (connectedUid.IsValid())
        {
            var delta = Transform(connectedUid).Coordinates.Position - xform.Coordinates.Position;
            _transform.SetLocalRotation(braceUid, delta.ToWorldAngle());
        }

        if (TryComp<AppearanceComponent>(braceUid, out _))
            _appearance.SetData(braceUid, ADTDrillBraceVisuals.Connected, connectedUid.IsValid());
    }

    private void TickDrill(EntityUid uid, ADTDrillComponent drill)
    {
        if (!HasValidBracing(uid, drill))
        {
            SystemError(uid, drill, Loc.GetString("drill-error-bracing"));
            return;
        }

        if (!HasPower(uid))
        {
            SystemError(uid, drill, Loc.GetString("drill-error-power"));
            return;
        }

        if (!IsOnLavaland(uid))
        {
            SystemError(uid, drill, Loc.GetString("drill-error-lavaland"));
            return;
        }

        var budget = _random.Next(drill.MinOrePerDeposit, drill.MaxOrePerDeposit + 1);
        AddOreToStorage(uid, drill, budget);

        if (drill.Error)
            return;

        TrySpawnDrillMob(uid);

        if (_random.Prob(drill.BreakChance))
        {
            SystemError(uid, drill, Loc.GetString("drill-error-breakdown"));
            return;
        }

        UpdateVisuals(uid, drill, refreshSupport: false);
    }

    private bool HasValidBracing(EntityUid uid, ADTDrillComponent drill)
        => Transform(uid).Anchored && CountBraces(uid) >= drill.BraceRequired;

    private bool HasPower(EntityUid uid)
        => TryComp<ApcPowerReceiverComponent>(uid, out var power) && power.Powered;

    private bool IsOnLavaland(EntityUid uid)
        => Transform(uid).MapUid is { } map && HasComp<ADTLavalandMapComponent>(map);

    private void AddOreToStorage(EntityUid uid, ADTDrillComponent drill, int budget)
    {
        if (budget <= 0)
            return;

        if (!_container.TryGetContainer(uid, drill.ContainerId, out var container))
            return;

        var distribution = _proto.Index<WeightedRandomOrePrototype>(drill.OreDistributionId);
        var xform = Transform(uid);

        while (budget > 0)
        {
            if (container.ContainedEntities.Count >= GetCapacity(drill))
            {
                SystemError(uid, drill, Loc.GetString("drill-error-storage"));
                return;
            }

            budget--;

            var oreId = distribution.Pick(_random);
            var ore = _proto.Index<OrePrototype>(oreId);

            if (ore.OreEntity == null)
                continue;

            var ent = Spawn(ore.OreEntity, xform.Coordinates);
            if (!_container.Insert(ent, container))
                QueueDel(ent);
        }
    }

    private void TrySpawnDrillMob(EntityUid uid)
    {
        if (!TryComp<ADTDrillSpawnerComponent>(uid, out var spawner) ||
            !_random.Prob(spawner.SpawnChance))
        {
            return;
        }

        var distribution = _proto.Index<WeightedRandomEntityPrototype>(spawner.SpawnTable);
        var entId = distribution.Pick(_random);

        var xform = Transform(uid);
        var offset = _random.NextVector2(spawner.SpawnRadius);
        Spawn(entId, xform.Coordinates.Offset(offset));
    }

    private void SystemError(EntityUid uid, ADTDrillComponent drill, string message)
    {
        if (drill.Active)
            _audio.PlayPvs(drill.StopSound, uid);

        drill.Active = false;
        drill.Error = true;
        _powerState.TrySetWorkingState(uid, false);
        _ambientSound.SetAmbience(uid, false, null);

        _popup.PopupEntity(message, uid);
        _audio.PlayPvs(drill.ErrorSound, uid);
        UpdateVisuals(uid, drill);
    }

    private void OnActivate(EntityUid uid, ADTDrillComponent drill, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<WiresPanelComponent>(uid, out var panel) && panel.Open)
        {
            _popup.PopupEntity(Loc.GetString("drill-panel-open"), uid, args.User);
            args.Handled = true;
            return;
        }

        if (drill.Error)
        {
            drill.Error = false;
            _popup.PopupEntity(Loc.GetString("drill-override-reset"), uid, args.User);
            UpdateVisuals(uid, drill);
            args.Handled = true;
            return;
        }

        if (!IsOnLavaland(uid))
        {
            _popup.PopupEntity(Loc.GetString("drill-error-lavaland"), uid, args.User);
            args.Handled = true;
            return;
        }

        if (!HasValidBracing(uid, drill))
        {
            _popup.PopupEntity(Loc.GetString("drill-error-bracing"), uid, args.User);
            args.Handled = true;
            return;
        }

        if (!HasPower(uid))
        {
            _popup.PopupEntity(Loc.GetString("drill-error-power"), uid, args.User);
            args.Handled = true;
            return;
        }

        drill.Active = !drill.Active;
        drill.NextTick = _timing.CurTime + drill.TickInterval;
        _powerState.TrySetWorkingState(uid, drill.Active);
        _ambientSound.SetAmbience(uid, drill.Active, null);

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(args.User)} toggled {ToPrettyString(uid)} {(drill.Active ? "on" : "off")}");

        _popup.PopupEntity(Loc.GetString(drill.Active ? "drill-start" : "drill-stop"), uid);
        _audio.PlayPvs(drill.Active ? drill.StartSound : drill.StopSound, uid);
        UpdateVisuals(uid, drill);
        args.Handled = true;
    }

    private void OnGetVerbs(EntityUid uid, ADTDrillComponent drill, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var verb = new Verb
        {
            Text = Loc.GetString("drill-unload-verb"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/drop.svg.192dpi.png")),
            Act = () => UnloadDrill(uid, drill, args.User),
            Impact = LogImpact.Low,
        };

        args.Verbs.Add(verb);
    }

    private void UnloadDrill(EntityUid uid, ADTDrillComponent drill, EntityUid user)
    {
        if (!_container.TryGetContainer(uid, drill.ContainerId, out var container) || container.ContainedEntities.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("drill-unload-empty"), uid, user);
            return;
        }

        EntityUid? oreBox = null;
        foreach (var ent in _lookup.GetEntitiesInRange(uid, drill.UnloadRange, LookupFlags.Dynamic | LookupFlags.Sundries))
        {
            if (TryComp<StorageComponent>(ent, out _))
            {
                oreBox = ent;
                break;
            }
        }

        if (oreBox == null)
        {
            _popup.PopupEntity(Loc.GetString("drill-unload-no-box"), uid, user);
            return;
        }

        var items = container.ContainedEntities.ToArray();
        var moved = 0;
        var rejected = 0;
        foreach (var item in items)
        {
            if (!_storage.CanInsert(oreBox.Value, item, out _))
            {
                rejected++;
                continue;
            }

            if (_storage.Insert(oreBox.Value, item, out _, user: user))
                moved++;
        }

        if (rejected > 0)
            _popup.PopupEntity(Loc.GetString("drill-unload-incompatible", ("count", rejected)), uid, user);
        else if (moved < items.Length)
            _popup.PopupEntity(Loc.GetString("drill-unload-full"), uid, user);

        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user)} unloaded {moved} items from {ToPrettyString(uid)} into {ToPrettyString(oreBox.Value)}");

        if (moved > 0)
            _popup.PopupEntity(Loc.GetString("drill-unload-success", ("count", moved)), uid, user);

        UpdateVisuals(uid, drill);
    }

    private void OnExamined(EntityUid uid, ADTDrillComponent drill, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var braces = CountBraces(uid);
        args.PushMarkup(Loc.GetString("drill-examine-braces", ("count", braces), ("required", drill.BraceRequired)));

        if (drill.Active)
            args.PushMarkup(Loc.GetString("drill-examine-active"));

        if (drill.Error)
            args.PushMarkup(Loc.GetString("drill-examine-error"));
    }

    private void OnComponentInit(EntityUid uid, ADTDrillComponent component, ComponentInit args)
    {
        _container.EnsureContainer<Container>(uid, component.ContainerId);
        RefreshAdjacentBraces(uid);
    }

    public int CountBraces(EntityUid uid)
    {
        var count = 0;
        ForEachAdjacentAnchored(Transform(uid), ent =>
        {
            if (HasComp<ADTDrillBraceComponent>(ent))
                count++;
        });
        return count;
    }

    internal void RefreshAdjacentDrills(EntityUid uid, TransformComponent xform, out EntityUid connectedUid)
    {
        var found = EntityUid.Invalid;
        ForEachAdjacentAnchored(xform, ent =>
        {
            if (!TryComp<ADTDrillComponent>(ent, out var drill))
                return;

            UpdateVisuals(ent, drill);

            if (xform.Anchored && !found.IsValid())
                found = ent;
        });
        connectedUid = found;
    }

    internal void ForEachAdjacentAnchored(TransformComponent xform, Action<EntityUid> action)
    {
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var tile = _map.LocalToTile(gridUid, grid, xform.Coordinates);

        foreach (var dir in CardinalDirs)
        {
            var enumerator = _map.GetAnchoredEntitiesEnumerator(gridUid, grid, tile + dir.ToIntVec());
            while (enumerator.MoveNext(out var ent))
                action(ent.Value);
        }
    }

    public int GetCapacity(ADTDrillComponent drill)
    {
        return Math.Max(1, drill.OreCapacity);
    }

    public int GetFillLevel(EntityUid uid, ADTDrillComponent drill)
    {
        if (!_container.TryGetContainer(uid, drill.ContainerId, out var container))
            return 0;

        var ratio = container.ContainedEntities.Count / (float)GetCapacity(drill);
        return Math.Clamp((int)MathF.Round(ratio * (MaxFillLevel + 1), MidpointRounding.AwayFromZero), 0, MaxFillLevel);
    }

    public void UpdateVisuals(EntityUid uid, ADTDrillComponent drill, bool refreshSupport = true)
    {
        if (!TryComp<AppearanceComponent>(uid, out _))
            return;

        _appearance.SetData(uid, ADTDrillVisuals.Active, drill.Active);
        _appearance.SetData(uid, ADTDrillVisuals.Error, drill.Error);
        _appearance.SetData(uid, ADTDrillVisuals.FillLevel, drill.Active && !drill.Error ? GetFillLevel(uid, drill) : -1);

        if (refreshSupport)
            _appearance.SetData(uid, ADTDrillVisuals.Supported, Transform(uid).Anchored && CountBraces(uid) >= drill.BraceRequired);
    }
}