using Content.Shared.ADT.Ninja;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Ninja.UI;

[UsedImplicitly]
public sealed class BrainExtractorBoundUserInterface : BoundUserInterface
{
    private BrainExtractorWindow? _window;

    public BrainExtractorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<BrainExtractorWindow>();
        _window.StartScanButton.OnPressed += _ => SendMessage(new BrainExtractorUiButtonPressedMessage(BrainExtractorUiButton.StartScan));
        _window.EjectButton.OnPressed += _ => SendMessage(new BrainExtractorUiButtonPressedMessage(BrainExtractorUiButton.Eject));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is BrainExtractorBoundUserInterfaceState cast)
            _window?.UpdateState(cast);
    }
}
