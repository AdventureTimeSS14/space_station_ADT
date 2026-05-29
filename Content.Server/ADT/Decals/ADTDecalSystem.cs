using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared.ADT.Decals;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Decals;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.ADT.Decals;

/// <summary>
///     Handles single decal removal requests from clients with admin permissions.
/// </summary>
public sealed class ADTDecalSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly Server.Decals.DecalSystem _decalSystem = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestSingleDecalRemovalEvent>(OnSingleDecalRemovalRequest);
    }

    private void OnSingleDecalRemovalRequest(RequestSingleDecalRemovalEvent ev, EntitySessionEventArgs eventArgs)
    {
        if (eventArgs.SenderSession is not { } session)
            return;

        if (!_adminManager.HasAdminFlag(session, AdminFlags.Spawn))
            return;

        var coordinates = GetCoordinates(ev.Coordinates);

        if (!coordinates.IsValid(EntityManager))
            return;

        var gridId = _transform.GetGrid(coordinates);

        if (gridId == null)
            return;

        foreach (var (decalId, decal) in _decalSystem.GetDecalsInRange(gridId.Value, ev.Coordinates.Position))
        {
            if (decal.Id != ev.DecalId)
                continue;

            if (eventArgs.SenderSession.AttachedEntity != null &&
                TryComp(eventArgs.SenderSession.AttachedEntity.Value, out MetaDataComponent? meta))
            {
                _adminLogger.Add(LogType.CrayonDraw, LogImpact.Low,
                    $"{ToPrettyString(eventArgs.SenderSession.AttachedEntity.Value):actor} removed a single {decal.Color} {decal.Id} at {ev.Coordinates}");
            }
            else
            {
                _adminLogger.Add(LogType.CrayonDraw, LogImpact.Low,
                    $"{eventArgs.SenderSession.Name} removed a single {decal.Color} {decal.Id} at {ev.Coordinates}");
            }

            _decalSystem.RemoveDecal(gridId.Value, decalId);
            break;
        }
    }
}
