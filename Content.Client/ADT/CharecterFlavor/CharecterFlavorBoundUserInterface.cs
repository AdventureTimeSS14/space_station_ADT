using Robust.Client.UserInterface;
using JetBrains.Annotations;
using Content.Shared.CharecterFlavor;

namespace Content.Client.ADT.CharecterFlavor;

[UsedImplicitly]
public sealed class CharecterFlavorBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private CharecterFlavorWindow? _window;


    public CharecterFlavorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CharecterFlavorWindow>();
        _window.OpenCentered();
        _window.SetEntity(Owner);
    }

}
