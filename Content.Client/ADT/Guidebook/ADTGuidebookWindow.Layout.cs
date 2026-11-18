using System.Numerics;
using Content.Client.ADT.UserInterface.Autoscroll;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.Guidebook;

public sealed partial class ADTGuidebookWindow
{
    private const float CompactWidth = 760f;
    private const float OutlineWidth = 1100f;
    private const float CompactToolbarWidth = 680f;
    private static readonly Vector2 DefaultSize = new(1280, 840);

    private readonly List<string> _categoryIds = new();
    private bool _sized;
    private OSWindow? _popout;
    private PanelContainer? _popoutBackground;
    private Vector2 _popinPosition;

    public bool IsPoppedOut => _popout != null;

    public event Action? PopoutClosed;

    private void InitializeLayout()
    {
        WindowBody.AddStyleClass(ADTMiddleClickAutoscrollController.StyleClassAutoscroll);
        WindowBody.OnResized += UpdateResponsiveLayout;
        PopoutButton.OnPressed += _ => TogglePopout();
    }

    public void OpenSized()
    {
        if (IsPoppedOut)
            return;

        if (_sized)
        {
            Open();
            UpdateResponsiveLayout();
            return;
        }

        _sized = true;
        var screen = UserInterfaceManager.WindowRoot.Size;
        var available = Vector2.Max(screen * 0.9f, MinSize);
        SetSize = Vector2.Min(DefaultSize, available);
        OpenCentered();
        UpdateResponsiveLayout();
    }

    private void TogglePopout()
    {
        if (IsPoppedOut)
            PopIn();
        else
            PopOut();
    }

    private void PopOut()
    {
        _popinPosition = Position;
        var popout = new OSWindow
        {
            SetSize = Size,
            Title = Title ?? Loc.GetString("guidebook-window-title"),
        };
        popout.Show();
        if (popout.Root == null)
        {
            popout.Close();
            return;
        }

        _popout = popout;
        _popoutBackground = new PanelContainer
        {
            StyleClasses = { "BackgroundPanel" },
        };
        WindowBody.Orphan();
        _popoutBackground.AddChild(WindowBody);
        popout.Root.AddChild(_popoutBackground);
        popout.Closed += OnPopoutClosed;
        PopoutButton.Text = Loc.GetString("adt-guidebook-pop-in");
        Close();
        UpdateResponsiveLayout();
    }

    private void PopIn()
    {
        if (_popout is not { } popout)
            return;

        popout.Closed -= OnPopoutClosed;
        RestoreBody();
        Open(_popinPosition);
        popout.Close();
        UpdateResponsiveLayout();
    }

    public void ClosePopout(bool notify = true)
    {
        if (_popout is not { } popout)
            return;

        popout.Closed -= OnPopoutClosed;
        RestoreBody();
        popout.Close();
        CancelSearch();
        if (notify)
            PopoutClosed?.Invoke();
    }

    private void OnPopoutClosed()
    {
        if (_popout == null)
            return;

        _popout.Closed -= OnPopoutClosed;
        RestoreBody();
        CancelSearch();
        PopoutClosed?.Invoke();
    }

    private void RestoreBody()
    {
        WindowBody.Orphan();
        ContentsContainer.AddChild(WindowBody);
        _popoutBackground?.Dispose();
        _popoutBackground = null;
        _popout = null;
        PopoutButton.Text = Loc.GetString("adt-guidebook-pop-out");
    }

    private void UpdateResponsiveLayout()
    {
        var width = WindowBody.Size.X;
        if (width <= 0)
            return;

        var compact = width < CompactWidth;
        CategorySidebar.Visible = !compact;
        SidebarDivider.Visible = !compact;
        CategoryMenu.Visible = compact;
        OutlinePanel.Visible = width >= OutlineWidth && OutlineList.ChildCount > 0;
        Toolbar.Orientation = width < CompactToolbarWidth
            ? BoxContainer.LayoutOrientation.Vertical
            : BoxContainer.LayoutOrientation.Horizontal;

        var popupWidth = Math.Max(1, Math.Min(480, width - 8));
        var popupHeight = Math.Max(1, Math.Min(260, WindowBody.Size.Y - 80));
        CategoryMenu.OptionsScroll.MaxWidth = popupWidth;
        CategoryMenu.OptionsScroll.MaxHeight = popupHeight;
    }

    private void SelectCategory(int id)
    {
        if (id == 0)
        {
            Navigate(new Page(PageKind.Home));
            return;
        }

        if (id > 0 && id <= _categoryIds.Count)
            OpenCategory(_categoryIds[id - 1]);
    }

    private void DisposeLayout()
    {
        WindowBody.OnResized -= UpdateResponsiveLayout;
        if (_popout is { } popout)
        {
            popout.Closed -= OnPopoutClosed;
            _popout = null;
            popout.Close();
        }
    }
}
