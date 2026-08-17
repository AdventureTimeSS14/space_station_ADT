using Content.Client.Eui;
using Content.Shared.ADT.Thunderdome;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.ADT.Thunderdome;

[UsedImplicitly]
public sealed partial class ThunderdomeLoadoutEui : BaseEui
{
    [Dependency] private readonly IEntityNetworkManager _net = default!;

    private readonly ThunderdomeLoadoutWindow _window;

    public ThunderdomeLoadoutEui()
    {
        _window = new ThunderdomeLoadoutWindow();
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
        _window.OnLoadoutConfirmed += weaponIdx =>
        {
            SendMessage(new ThunderdomeLoadoutSelectedMessage(weaponIdx));
        };

        _window.OnLeaderboardRequested += () =>
        {
            _net.SendSystemNetworkMessage(new ThunderdomeLeaderboardRequestEvent());
        };
    }

    public override void Opened()
    {
        base.Opened();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);

        if (state is not ThunderdomeLoadoutEuiState loadoutState)
            return;

        _window.UpdateState(loadoutState);
    }
}
