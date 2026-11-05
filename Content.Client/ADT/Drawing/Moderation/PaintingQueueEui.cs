using Content.Client.Eui;
using Content.Shared.ADT.Drawing;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.ADT.Drawing.Moderation;

[UsedImplicitly]
public sealed class PaintingQueueEui : BaseEui
{
    private readonly PaintingQueueWindow _window;

    public PaintingQueueEui()
    {
        _window = new PaintingQueueWindow();
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
        _window.MessageRequested += SendMessage;
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is PaintingQueueEuiState queue)
            _window.UpdateState(queue);
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
