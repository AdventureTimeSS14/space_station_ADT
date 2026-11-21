using Content.Shared.ADT.Heretic;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Heretic.UI;

public sealed class PaintingCurseBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private PaintingCurseWindow? _window;

    public PaintingCurseBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PaintingCurseWindow>();
        _window.OnTargetSelected += target => SendMessage(new PaintingCurseSelectMessage(target));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is PaintingCurseBuiState curseState)
            _window?.SetTargets(curseState.Targets);
    }
}
