using Content.Client.UserInterface.Controls;
using Content.Shared.ADT.Surgery.Events;
using JetBrains.Annotations;

namespace Content.Client.ADT.Surgery.UI;

[UsedImplicitly]
public sealed class SurgeryBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private SurgeryWindow? _window;

    [ViewVariables]
    private SimpleRadialMenu? _radial;

    public SurgeryBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        switch (state)
        {
            case SurgeryChooseGraphState graphState:
                OpenGraphList(graphState);
                break;

            case SurgeryChooseEdgeState edgeState:
                OpenEdgeRadial(edgeState);
                break;
        }
    }

    private void OpenGraphList(SurgeryChooseGraphState state)
    {
        CloseRadial();

        if (_window == null)
        {
            _window = new SurgeryWindow
            {
                Title = Loc.GetString("surgery-window-title"),
            };
            _window.OnClose += Close;
            _window.OnGraphSelected += OnGraphSelected;
        }

        _window.Populate(state);
        _window.OpenCentered();
    }

    private void OnGraphSelected(string graphId)
    {
        SendMessage(new SurgerySelectGraphMessage(graphId));
        _window?.Close();
    }

    private void OpenEdgeRadial(SurgeryChooseEdgeState state)
    {
        _window?.Close();
        CloseRadial();

        var options = new List<RadialMenuOptionBase>();
        foreach (var (edgeId, label, iconEntity, icon) in state.Options)
        {
            var entity = iconEntity != null ? EntMan.GetEntity(iconEntity.Value) : (EntityUid?)null;

            options.Add(new RadialMenuActionOption<string>(OnEdgeSelected, edgeId)
            {
                ToolTip = label,
                IconSpecifier = entity != null ? RadialMenuIconSpecifier.With(entity) : RadialMenuIconSpecifier.With(icon),
                OverlayIcon = entity != null ? icon : null,
            });
        }

        _radial = new SimpleRadialMenu();
        _radial.SetButtons(options);
        _radial.Track(Owner);
        _radial.OpenOverMouseScreenPosition();
    }

    private void OnEdgeSelected(string edgeId)
    {
        SendMessage(new SurgerySelectEdgeMessage(edgeId));
    }

    private void CloseRadial()
    {
        _radial?.Dispose();
        _radial = null;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        if (_window != null)
        {
            _window.OnClose -= Close;
            _window.OnGraphSelected -= OnGraphSelected;
            _window.Dispose();
        }

        CloseRadial();
    }
}
