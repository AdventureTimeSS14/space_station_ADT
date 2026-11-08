using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Storage.Controls;

public sealed partial class StorageWindow
{
    private Control? _adtTrackedParent;

    protected override void EnteredTree()
    {
        base.EnteredTree();

        if (Parent == null)
            return;

        _adtTrackedParent = Parent;
        _adtTrackedParent.OnResized += OnAdtParentResized;
    }

    protected override void ExitedTree()
    {
        if (_adtTrackedParent != null)
            _adtTrackedParent.OnResized -= OnAdtParentResized;

        _adtTrackedParent = null;
        base.ExitedTree();
    }

    private void OnAdtParentResized()
    {
        if (_adtTrackedParent == null || Parent != _adtTrackedParent || Parent is LayoutContainer)
            return;

        InvalidateArrange();
        Parent.InvalidateArrange();
    }
}
