using Content.Server.Power.Components;
using Content.Server.Popups;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.ADT.Artillery;
using Content.Shared.Pinpointer;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using System.Linq;
using System.Globalization;

namespace Content.Server.ADT.Artillery;

public sealed class ArtilleryConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ArtilleryConsoleComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ArtilleryConsoleComponent, ArtilleryConsoleSelectBeaconMessage>(OnSelectBeacon);
        SubscribeLocalEvent<ArtilleryConsoleComponent, ArtilleryConsoleFireMessage>(OnFire);
        SubscribeLocalEvent<ArtilleryConsoleComponent, ArtilleryConsoleToggleMessage>(OnToggle);
    }

    public override void Update(float frameTime)
    {
        var consoleQuery = EntityQueryEnumerator<ArtilleryConsoleComponent>();
        while (consoleQuery.MoveNext(out var uid, out var comp))
        {
            comp.UiUpdateAccumulator += frameTime;
            if (comp.UiUpdateAccumulator < comp.UiUpdateInterval)
                continue;

            comp.UiUpdateAccumulator -= comp.UiUpdateInterval;

            if (_ui.IsUiOpen(uid, ArtilleryConsoleUiKey.Key))
                UpdateUi(uid, comp);
        }
    }

    private void OnUiOpened(EntityUid uid, ArtilleryConsoleComponent comp, BoundUIOpenedEvent args)
    {
        UpdateUi(uid, comp);
    }

    private void OnSelectBeacon(EntityUid uid, ArtilleryConsoleComponent comp, ArtilleryConsoleSelectBeaconMessage msg)
    {
        comp.SelectedBeacon = msg.Beacon;
        UpdateUi(uid, comp);
    }

    private void OnFire(EntityUid uid, ArtilleryConsoleComponent comp, ArtilleryConsoleFireMessage msg)
    {
        if (msg.Actor is not { Valid: true } actor)
            return;

        if (!CanUse(actor, uid))
        {
            _popup.PopupEntity(Loc.GetString("bluespace-artillery-permission-denied"), uid, actor);
            return;
        }

        if (!TryGetSelectedBeacon(comp, out var beaconUid))
            return;

        if (!TryFindCannon(uid, out var cannonUid, out var cannon))
            return;

        if (GetCooldownRemaining(cannon) > TimeSpan.Zero)
            return;

        cannon.NextFireTime = _timing.CurTime + TimeSpan.FromMinutes(cannon.CooldownMinutes);

        var mapCoords = _transform.GetMapCoordinates(beaconUid);

        var fireEvent = new ArtilleryCannonFireEvent(mapCoords);
        RaiseLocalEvent(cannonUid, ref fireEvent);

        UpdateUi(uid, comp);
    }

    private void OnToggle(EntityUid uid, ArtilleryConsoleComponent comp, ArtilleryConsoleToggleMessage msg)
    {
        if (msg.Actor is not { Valid: true } actor)
            return;

        if (!CanUse(actor, uid))
        {
            _popup.PopupEntity(Loc.GetString("bluespace-artillery-permission-denied"), uid, actor);
            return;
        }

        if (!TryFindCannon(uid, out _, out var cannon))
            return;

        if (cannon.IsCharging)
            return;

        cannon.IsEnabled = !cannon.IsEnabled;
        UpdateUi(uid, comp);
    }

    private void UpdateUi(EntityUid uid, ArtilleryConsoleComponent comp)
    {
        var beacons = GetBeaconList(uid);
        var selected = comp.SelectedBeacon;
        if (selected == NetEntity.Invalid || !beacons.Any(b => b.NetEntity == selected))
        {
            selected = beacons.Count > 0 ? beacons[0].NetEntity : NetEntity.Invalid;
            comp.SelectedBeacon = selected;
        }

        var hasCannon = TryFindCannon(uid, out var cannonUid, out var cannon);
        var cooldownRemaining = hasCannon ? GetCooldownRemaining(cannon) : TimeSpan.Zero;

        var receivedPower = 0f;
        var requiredPower = 0f;
        var fullyPowered = false;

        if (hasCannon && TryComp<PowerConsumerComponent>(cannonUid, out var powerConsumer))
        {
            receivedPower = powerConsumer.ReceivedPower;
            requiredPower = cannon.PowerUseActive;
            fullyPowered = cannon.IsEnabled && receivedPower >= requiredPower;
        }

        var canFire = hasCannon && cooldownRemaining <= TimeSpan.Zero && selected != NetEntity.Invalid && cannon.IsEnabled && fullyPowered;
        var canToggle = hasCannon && !cannon.IsCharging;
        var cannonEnabled = hasCannon && cannon.IsEnabled;

        _ui.SetUiState(uid, ArtilleryConsoleUiKey.Key,
            new ArtilleryConsoleBoundUserInterfaceState(beacons, selected, cooldownRemaining, hasCannon, canFire, canToggle, cannonEnabled, receivedPower, requiredPower, fullyPowered));
    }

    private List<ArtilleryBeaconEntry> GetBeaconList(EntityUid uid)
    {
        var list = new List<ArtilleryBeaconEntry>();

        if (!TryFindCannon(uid, out var cannonUid, out var cannon))
            return list;

        if (!TryComp<TransformComponent>(cannonUid, out var cannonXform))
            return list;

        var cannonMapId = cannonXform.MapID;

        var query = EntityQueryEnumerator<NavMapBeaconComponent, TransformComponent>();
        while (query.MoveNext(out var beaconUid, out var beacon, out var xform))
        {
            if (!beacon.Enabled || xform.GridUid == null || !xform.Anchored)
                continue;

            if (!cannon.MultiMap && xform.MapID != cannonMapId)
                continue;

            var name = beacon.Text;
            if (string.IsNullOrEmpty(name))
                name = MetaData(beaconUid).EntityName;

            list.Add(new ArtilleryBeaconEntry(GetNetEntity(beaconUid), name));
        }

        list.Sort((a, b) => string.Compare(a.Name, b.Name, true, CultureInfo.CurrentCulture));
        return list;
    }

    private bool TryGetSelectedBeacon(ArtilleryConsoleComponent comp, out EntityUid beaconUid)
    {
        beaconUid = default;

        if (comp.SelectedBeacon == NetEntity.Invalid)
            return false;

        if (!EntityManager.TryGetEntity(comp.SelectedBeacon, out var resolved))
            return false;

        beaconUid = resolved.Value;
        return true;
    }

    private bool TryFindCannon(EntityUid uid, out EntityUid cannonUid, out ArtilleryCannonComponent cannon)
    {
        cannonUid = default;
        cannon = default!;

        if (!TryComp(uid, out TransformComponent? consoleXform))
            return false;

        var consoleMap = consoleXform.MapID;
        var consolePos = _transform.GetWorldPosition(uid);
        var maxDistance = 20f;
        var closestDistance = maxDistance;

        var query = EntityQueryEnumerator<ArtilleryCannonComponent, TransformComponent>();
        while (query.MoveNext(out var qUid, out var qCannon, out var qXform))
        {
            if (qXform.MapID != consoleMap)
                continue;

            var cannonPos = _transform.GetWorldPosition(qUid);
            var distance = (cannonPos - consolePos).Length();

            if (distance > maxDistance || distance > closestDistance)
                continue;

            cannonUid = qUid;
            cannon = qCannon;
            closestDistance = distance;
        }

        return cannonUid != default;
    }

    private TimeSpan GetCooldownRemaining(ArtilleryCannonComponent cannon)
    {
        var remaining = cannon.NextFireTime - _timing.CurTime;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private bool CanUse(EntityUid user, EntityUid console)
    {
        if (TryComp<AccessReaderComponent>(console, out var accessReader))
            return _access.IsAllowed(user, console, accessReader);

        return true;
    }
}
