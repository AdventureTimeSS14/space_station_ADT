using Content.Client.Eui;
using Content.Shared.ADT.Polls;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Polls.UI;

[UsedImplicitly]
public sealed class PollPanelEui : BaseEui
{
    [Dependency] private IPrototypeManager _proto = default!;

    private readonly PollPanelWindow _window;

    public PollPanelEui()
    {
        IoCManager.InjectDependencies(this);

        _window = new PollPanelWindow(_proto);

        _window.OnClose += () => SendMessage(new CloseEuiMessage());

        _window.RefreshRequested += () => SendMessage(new PollPanelEuiMsg.Refresh());

        _window.SaveRequested += poll =>
        {
            SendMessage(new PollPanelEuiMsg.Save
            {
                Poll = poll,
            });
        };

        _window.SetClosedRequested += (id, closed) =>
        {
            SendMessage(new PollPanelEuiMsg.SetClosed
            {
                PollId = id,
                Closed = closed,
            });
        };

        _window.DeleteRequested += id =>
        {
            SendMessage(new PollPanelEuiMsg.Delete
            {
                PollId = id,
            });
        };

        _window.WipeRequested += id =>
        {
            SendMessage(new PollPanelEuiMsg.Wipe
            {
                PollId = id,
            });
        };

        _window.StatsRequested += (id, groupBy, page) =>
        {
            SendMessage(new PollPanelEuiMsg.RequestStats
            {
                PollId = id,
                GroupBy = groupBy,
                Page = page,
            });
        };
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not PollPanelEuiState s)
            return;

        _window.SetState(s);
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
        _window.Dispose();
    }
}
