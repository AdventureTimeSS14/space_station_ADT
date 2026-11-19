using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Guidebook.Lazy;

public class ADTLazyList : BoxContainer
{
    private const int MaterializePerPass = 8;

    private readonly List<ADTLazySlot> _slots = new();
    private ScrollContainer? _scroll;
    private bool _queued;

    public event Action<Control>? Materialized;

    public ADTLazyList()
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;
        MouseFilter = MouseFilterMode.Stop;
        OnResized += QueueCheck;
    }

    public void AddLazy(Func<Control> factory, string searchText, IPrototype? prototype, float estimatedHeight)
    {
        var slot = new ADTLazySlot(this, factory, searchText, prototype, estimatedHeight);
        _slots.Add(slot);
        AddChild(slot);
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        _scroll = FindScroll();
        if (_scroll != null)
            _scroll.OnScrolled += QueueCheck;

        QueueCheck();
    }

    protected override void ExitedTree()
    {
        if (_scroll != null)
            _scroll.OnScrolled -= QueueCheck;

        _scroll = null;
        base.ExitedTree();
    }

    private ScrollContainer? FindScroll()
    {
        var parent = Parent;
        while (parent != null)
        {
            if (parent is ScrollContainer scroll)
                return scroll;

            parent = parent.Parent;
        }

        return null;
    }

    internal void QueueCheck()
    {
        if (_queued)
            return;

        _queued = true;
        UserInterfaceManager.DeferAction(Check);
    }

    internal void OnMaterialized(Control control)
    {
        Materialized?.Invoke(control);
    }

    private void Check()
    {
        _queued = false;
        if (Disposed || !IsInsideTree || !VisibleInTree)
            return;

        float top;
        float bottom;
        if (_scroll != null)
        {
            top = _scroll.GlobalPosition.Y;
            bottom = top + _scroll.Size.Y;
        }
        else
        {
            top = GlobalPosition.Y;
            bottom = top + Size.Y;
        }

        var margin = bottom - top;
        top -= margin;
        bottom += margin;

        var created = 0;
        foreach (var slot in _slots)
        {
            if (slot.IsMaterialized || !slot.Visible)
                continue;

            var y = slot.GlobalPosition.Y;
            if (y > bottom)
                break;

            if (y + slot.Size.Y < top)
                continue;

            slot.Materialize();
            if (++created >= MaterializePerPass)
            {
                QueueCheck();
                return;
            }
        }
    }
}
