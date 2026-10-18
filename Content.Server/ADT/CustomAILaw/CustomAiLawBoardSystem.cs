using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;
using Content.Server.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Server.Administration.Managers;
using Content.Server.EUI;
using Robust.Server.Player;

namespace Content.Server.ADT.CustomAiLawBoard;

public sealed class CustomAiLawBoardSystem : EntitySystem
{
    [Dependency] private SharedToolSystem _toolSystem = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private SiliconLawSystem _siliconLawSystem = default!;
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private EuiManager _euiManager = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CustomAiLawBoardComponent, InteractUsingEvent>(OnInteractUsing);
    }
    private void OnInteractUsing(EntityUid uid, CustomAiLawBoardComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!_toolSystem.HasQuality(args.Used, SharedToolSystem.PulseQuality))
            return;

        if (!TryComp<SiliconLawBoundComponent>(uid, out var lawBoundComponent))
            return;
        var ui = new SiliconLawEui(_siliconLawSystem, EntityManager, _adminManager);
        if (!_playerManager.TryGetSessionByEntity(args.User, out var session))
        {
            return;
        }
        _euiManager.OpenEui(ui, session);
        ui.UpdateLaws(lawBoundComponent, args.Target);
    }
}
