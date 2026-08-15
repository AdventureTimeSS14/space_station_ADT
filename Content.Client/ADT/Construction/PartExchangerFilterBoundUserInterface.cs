using Content.Client.UserInterface.Controls;
using Content.Shared.ADT.Construction;
using Content.Shared.ADT.Construction.Prototypes;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.Construction;
public sealed class PartExchangerFilterBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private SimpleRadialMenu? _menu;
    private List<ProtoId<MachinePartPrototype>> _available = new();
    private List<ProtoId<MachinePartPrototype>> _selected = new();

    public PartExchangerFilterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Open()
    {
        base.Open();

        _menu = new SimpleRadialMenu();
        _menu.OnClose += Close;
        _menu.ContextualButton.OnButtonUp += _ => ResetFilter();
        _menu.OpenOverMouseScreenPosition();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not PartExchangerFilterState filter)
            return;

        _available = filter.AvailableParts;
        _selected = filter.SelectedParts;
        Rebuild();
    }

    private void Rebuild()
    {
        if (_menu == null)
            return;

        var models = new List<RadialMenuOptionBase>();
        foreach (var partId in _available)
        {
            var partProto = _prototype.Index(partId);

            models.Add(new RadialMenuActionOption<ProtoId<MachinePartPrototype>>(TogglePart, partId)
            {
                ToolTip = Loc.GetString(partProto.Name),
                IconSpecifier = RadialMenuIconSpecifier.With(partProto.StockPartPrototype),
                BackgroundColor = _selected.Contains(partId)
                    ? Color.FromHex("#2f8f45")
                    : Color.FromHex("#4a4a4a"),
            });
        }

        _menu.SetButtons(models, new SimpleRadialMenuSettings
        {
            DefaultContainerRadius = 70,
        });
    }

    private void TogglePart(ProtoId<MachinePartPrototype> partId)
    {
        var selection = new List<ProtoId<MachinePartPrototype>>(_selected);
        if (selection.Remove(partId) == false)
            selection.Add(partId);

        SendFilterMessage(selection);
    }

    private void ResetFilter()
    {
        if (_selected.Count == 0)
            return;

        SendFilterMessage(new List<ProtoId<MachinePartPrototype>>());
    }

    private void SendFilterMessage(List<ProtoId<MachinePartPrototype>> selectedParts)
    {
        SendMessage(new PartExchangerFilterChangedMessage { SelectedParts = selectedParts });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (_menu != null)
        {
            _menu.OnClose -= Close;
            _menu.Dispose();
        }
    }
}
