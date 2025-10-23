using Content.Shared.ADT.Xenobiology.UI;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.Xenobiology.UI;

[UsedImplicitly]
public sealed class SlimeAnalyzerBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SlimeAnalyzerWindow? _window;

    public SlimeAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<SlimeAnalyzerWindow>();

        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (_window == null)
            return;

        if (message is SlimeAnalyzerScannedUserMessage msg)
            _window?.Populate(msg);
    }
}
