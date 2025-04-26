using Robust.Client.UserInterface;
using JetBrains.Annotations;
using Content.Shared.CrewManifest;
using Content.Shared.ADT.Silicons.StationAi;

namespace Content.Client.ADT.Silicons.StationAi;

public sealed class StationAiBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private StationAiInfo? _window;

    public StationAiBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<StationAiInfo>();
        _window.CrewManifestButton.OnPressed += _ => SendMessage(new CrewManifestOpenUiMessage());
    }
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not StationAiUpdateState updateState || _window == null)
            return;

        _window.UpdateState(updateState);
    }
}
