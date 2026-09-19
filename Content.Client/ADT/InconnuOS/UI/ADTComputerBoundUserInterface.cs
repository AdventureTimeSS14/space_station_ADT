using Content.Shared.ADT.InconnuOS;
using JetBrains.Annotations;
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.ADT.InconnuOS.UI;

[UsedImplicitly]
public sealed class ADTComputerBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private InconnuOsWindow? _window;

    public ADTComputerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        TryCreateWindow();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not ADTOsBuiState current)
            return;

        if (_window == null)
        {
            CreateWindow(current);
            return;
        }

        _window.SetState(current);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        _window?.ReceiveMessage(message);
    }

    private void TryCreateWindow()
    {
        if (_window != null || State is not ADTOsBuiState current)
            return;

        CreateWindow(current);
    }

    private void CreateWindow(ADTOsBuiState state)
    {
        var system = EntMan.System<ADTOsSystem>();

        _window = new InconnuOsWindow(
            _prototypes,
            _cache,
            _timing,
            EntMan,
            system.Apps,
            state,
            system.GetSession(Owner));

        _window.OnClose += Close;
        _window.OnMessage += SendMessage;
        _window.OnSessionReset += () => system.ClearSession(Owner);

        _window.OpenCentered();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing || _window == null)
            return;

        EntMan.System<ADTOsSystem>().SaveSession(Owner, _window.SaveSession());

        _window.Dispose();
        _window = null;
    }
}
