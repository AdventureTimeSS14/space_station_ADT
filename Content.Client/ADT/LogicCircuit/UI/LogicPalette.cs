using Content.Shared.ADT.LogicCircuit;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.ADT.LogicCircuit.UI;

public sealed class LogicPalette : BoxContainer
{
    private readonly IPrototypeManager _prototypes;
    private readonly BoxContainer _list;
    private readonly LineEdit _filter;

    public event Action<LogicElementPrototype>? OnElementPicked;

    public LogicPalette(IPrototypeManager prototypes)
    {
        _prototypes = prototypes;

        Orientation = LayoutOrientation.Vertical;
        MinHeight = 190f;

        var header = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            Margin = new Thickness(4f, 4f, 4f, 2f),
        };

        header.AddChild(new Label
        {
            Text = Loc.GetString("logic-circuit-palette-filter"),
            Margin = new Thickness(0f, 0f, 6f, 0f),
        });

        _filter = new LineEdit
        {
            HorizontalExpand = true,
            PlaceHolder = Loc.GetString("logic-circuit-palette-filter-hint"),
        };

        _filter.OnTextChanged += _ => Rebuild();
        header.AddChild(_filter);
        AddChild(header);

        _list = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
        };

        AddChild(new ScrollContainer
        {
            VerticalExpand = true,
            HScrollEnabled = false,
            Children = { _list },
        });

        Rebuild();
    }

    private void Rebuild()
    {
        _list.RemoveAllChildren();

        var filter = _filter.Text.Trim();

        foreach (var category in Enum.GetValues<LogicElementCategory>())
        {
            var grid = new GridContainer
            {
                Columns = 6,
                Margin = new Thickness(4f, 0f, 4f, 6f),
            };

            foreach (var proto in _prototypes.EnumeratePrototypes<LogicElementPrototype>())
            {
                if (proto.Hidden || proto.Category != category)
                    continue;

                var name = Loc.GetString(proto.Name);

                if (filter.Length > 0 && !name.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
                    continue;

                grid.AddChild(MakeButton(proto, name));
            }

            if (grid.ChildCount == 0)
                continue;

            _list.AddChild(new Label
            {
                Text = Loc.GetString($"logic-category-{category.ToString().ToLowerInvariant()}"),
                Margin = new Thickness(6f, 4f, 0f, 2f),
                Modulate = LogicCircuitStyle.TextDim,
            });

            _list.AddChild(grid);
        }
    }

    private Button MakeButton(LogicElementPrototype proto, string name)
    {
        var button = new Button
        {
            Text = $"{name}  [{proto.Cost}]",
            ClipText = true,
            SetWidth = 156f,
            ToolTip = proto.Description == null ? null : Loc.GetString(proto.Description.Value),
            ModulateSelfOverride = proto.Color,
        };

        button.OnPressed += _ => OnElementPicked?.Invoke(proto);
        return button;
    }
}
