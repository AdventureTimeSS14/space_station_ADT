using Content.Client.Eui;
using Content.Shared.ADT.Administration.PlayersDashboard;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.ADT.Administration.UI.PlayersDashboard;

[UsedImplicitly]
public sealed class PlayersDashboardEui : BaseEui
{
    private readonly PlayersDashboardWindow _window;

    public PlayersDashboardEui()
    {
        _window = new PlayersDashboardWindow();
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is PlayersDashboardEuiState cast)
            _window.UpdateState(cast);
    }
}
