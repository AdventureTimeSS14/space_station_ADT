using Content.Client.ADT.Xenobiology.UI;
using Content.Shared.ADT.Xenobiology.XenobiologyControlConsole;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameStates;
using Robust.Shared.Player;

namespace Content.Client.ADT.Xenobiology;

/// <summary>
/// Shows the xenobiology console status counter (monkeys and slimes) while piloting the console eye.
/// </summary>
public sealed partial class XenobiologyConsoleControlSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private XenobiologyConsoleStatusControl? _status;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenobiologyConsoleViewComponent, ComponentStartup>(OnViewStartup);
        SubscribeLocalEvent<XenobiologyConsoleViewComponent, ComponentShutdown>(OnViewShutdown);
        SubscribeLocalEvent<XenobiologyConsoleViewComponent, AfterAutoHandleStateEvent>(OnViewState);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);

        _ui.OnScreenChanged += OnScreenChanged;
    }

    public override void Shutdown()
    {
        _ui.OnScreenChanged -= OnScreenChanged;
        RemoveStatus();
        base.Shutdown();
    }

    private void OnViewStartup(Entity<XenobiologyConsoleViewComponent> ent, ref ComponentStartup args)
    {
        if (_player.LocalEntity == ent.Owner)
            AddStatus(ent.Comp);
    }

    private void OnViewShutdown(Entity<XenobiologyConsoleViewComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity == ent.Owner)
            RemoveStatus();
    }

    private void OnViewState(Entity<XenobiologyConsoleViewComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_player.LocalEntity == ent.Owner)
            UpdateStatus(ent.Comp);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        if (TryComp<XenobiologyConsoleViewComponent>(args.Entity, out var view))
            AddStatus(view);
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        RemoveStatus();
    }

    private void OnScreenChanged((UIScreen? Old, UIScreen? New) args)
    {
        RemoveStatus();

        if (args.New != null &&
            _player.LocalEntity is { } player &&
            TryComp<XenobiologyConsoleViewComponent>(player, out var view))
        {
            AddStatus(view);
        }
    }

    private void AddStatus(XenobiologyConsoleViewComponent view)
    {
        if (_status == null)
        {
            if (_ui.ActiveScreen is not { } screen)
                return;

            _status = new XenobiologyConsoleStatusControl();
            screen.AddChild(_status);
            LayoutContainer.SetAnchorAndMarginPreset(
                _status,
                LayoutContainer.LayoutPreset.CenterLeft,
                margin: 12);
        }

        UpdateStatus(view);
    }

    private void UpdateStatus(XenobiologyConsoleViewComponent view)
    {
        _status?.UpdateState(view.StoredSlimes, view.MaxStoredSlimes, view.MonkeyCubes);
    }

    private void RemoveStatus()
    {
        _status?.Orphan();
        _status = null;
    }
}
