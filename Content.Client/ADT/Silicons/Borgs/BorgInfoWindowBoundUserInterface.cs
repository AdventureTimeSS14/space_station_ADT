using Robust.Client.UserInterface;
using JetBrains.Annotations;
using Content.Shared.ADT.Silicons.Borgs;
using Content.Shared.CrewManifest;

namespace Content.Client.ADT.Silicons.Borgs;

[UsedImplicitly]
public sealed class BorgInfoBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private BorgInfoWindow? _window;

    public BorgInfoBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<BorgInfoWindow>();
        _window.SetEntity(Owner);
        _window.CrewManifestButton.OnPressed += _ => SendMessage(new CrewManifestOpenUiMessage());
    }
    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not BorgInfoUpdateState updateState)
            return;

        if (_window == null)
        {
            return;
        }

        _window.UpdateState(updateState);
        _window.UpdateModulePanel(Owner);
    }
}
