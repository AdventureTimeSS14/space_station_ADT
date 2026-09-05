using Content.Client.ADT.CartridgeLoader.Cartridges;
using Content.Shared.ADT.CartridgeLoader.Cartridges;
using Content.Shared.ADT.NanoChat;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.NanoChat;

[UsedImplicitly]
public sealed class StationAiNanoChatBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private StationAiNanoChatWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<StationAiNanoChatWindow>();
        _window.Fragment.OnMessageSent += OnMessageSent;
    }

    private void OnMessageSent(NanoChatUiMessageType type, uint? number, string? content, string? job)
    {
        SendMessage(new StationAiNanoChatUiMessage(type, number, content, job));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not NanoChatUiState nanoChatState || _window == null)
            return;

        _window.UpdateState(nanoChatState);
    }

    protected override void Dispose(bool disposing)
    {
        if (_window != null)
        {
            _window.Fragment.OnMessageSent -= OnMessageSent;
            _window.Dispose();
            _window = null;
        }

        base.Dispose(disposing);
    }
}
