using System.Numerics;
using Content.Client.ADT.CartridgeLoader.Cartridges;
using Content.Shared.ADT.CartridgeLoader.Cartridges;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.ADT.NanoChat;

public sealed class StationAiNanoChatWindow : DefaultWindow
{
    public readonly NanoChatUiFragment Fragment;

    public StationAiNanoChatWindow()
    {
        Title = Loc.GetString("station-ai-nanochat-window-title");
        MinSize = new Vector2(600, 400);

        Fragment = new NanoChatUiFragment();
        Contents.AddChild(Fragment);
    }

    public void UpdateState(NanoChatUiState state)
    {
        Fragment.UpdateState(state);
    }
}
