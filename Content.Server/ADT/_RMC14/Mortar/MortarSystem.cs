using System.Numerics;
using Content.Server.Popups;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Mortar;
using Content.Shared.Maps;
using Robust.Server.Containers;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using static Content.Shared.Popups.PopupType;

namespace Content.Server._RMC14.Mortar;

public sealed class MortarSystem : SharedMortarSystem
{
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRMCMapSystem _rmcMap = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    protected override bool CanLoadPopup(
        Entity<MortarComponent> mortar,
        Entity<MortarShellComponent> shell,
        EntityUid user,
        out TimeSpan travelTime,
        out MapCoordinates coordinates)
    {
        travelTime = default;
        coordinates = default;

        if (!mortar.Comp.Deployed)
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-not-deployed", ("mortar", mortar)), user, user, SmallCaution);
            return false;
        }

        var time = _timing.CurTime;
        if (time < mortar.Comp.LastFiredAt + mortar.Comp.FireDelay)
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-fire-cooldown", ("mortar", mortar)), user, user, SmallCaution);
            return false;
        }

        var target = mortar.Comp.Target + mortar.Comp.Offset + mortar.Comp.Dial;
        if (target == Vector2i.Zero)
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-not-aimed", ("mortar", mortar)), user, user, SmallCaution);
            return false;
        }

        var mortarCoordinates = _transform.GetMapCoordinates(mortar);
        coordinates = new MapCoordinates(Vector2.Zero, mortarCoordinates.MapId);

        // Упрощённая логика обработки координат
        coordinates = coordinates.Offset(target);

        // Упрощённая проверка времени полёта
        if (coordinates.MapId == mortarCoordinates.MapId) // Проверка на принадлежность к той же карте
        {
            travelTime = shell.Comp.TravelDelay;
        }

        if ((mortarCoordinates.Position - coordinates.Position).Length() < mortar.Comp.MinimumRange)
        {
            _popup.PopupEntity(Loc.GetString("rmc-mortar-target-too-close"), user, user, SmallCaution);
            return false;
        }

        var deviation = shell.Comp.PlanetDeviation;
        var xDeviation = _random.Next(-deviation, deviation + 1);
        var yDeviation = _random.Next(-deviation, deviation + 1);
        coordinates = coordinates.Offset(new Vector2(xDeviation, yDeviation));

        if (_container.TryGetContainer(mortar, mortar.Comp.ContainerId, out var container) &&
            !_container.CanInsert(shell, container))
        {
            _popup.PopupClient(Loc.GetString("rmc-mortar-cant-insert", ("shell", shell), ("mortar", mortar)), user, user, SmallCaution);
            return false;
        }

        return true;
    }
}
