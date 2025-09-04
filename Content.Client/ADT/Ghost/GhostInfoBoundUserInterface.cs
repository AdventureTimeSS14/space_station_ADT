using Robust.Client.UserInterface;
using JetBrains.Annotations;
using Content.Shared.ADT.Ghost;
using Content.Shared.CrewManifest;

namespace Content.Client.ADT.Ghost;

[UsedImplicitly]
public sealed class GhostInfoBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private GhostInfo? _window;

    public GhostInfoBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {

        base.Open();

        _window = this.CreateWindow<GhostInfo>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not GhostInfoUpdateState updateState || _window == null)
            return;

        _window.UpdateState(updateState);
    }
}
