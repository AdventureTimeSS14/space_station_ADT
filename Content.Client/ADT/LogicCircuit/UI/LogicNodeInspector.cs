using System.Globalization;
using Content.Shared.ADT.LogicCircuit;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.LogicCircuit.UI;

public sealed class LogicNodeInspector : BoxContainer
{
    private const float PanelWidth = 280f;

    private readonly Label _title;
    private readonly RichTextLabel _description;
    private readonly BoxContainer _fields;
    private readonly Button _delete;

    private LogicNodeControl? _node;
    private int _portCount = 1;

    public event Action<LogicNodeControl>? OnDeleteRequested;
    public event Action? OnConfigChanged;

    public LogicNodeInspector()
    {
        Orientation = LayoutOrientation.Vertical;
        SetWidth = PanelWidth;
        HorizontalExpand = false;
        RectClipContent = true;
        Margin = new Thickness(6f, 4f, 4f, 4f);

        _title = new Label
        {
            Text = Loc.GetString("logic-circuit-inspector-empty"),
            Margin = new Thickness(0f, 0f, 0f, 4f),
            ClipText = true,
        };

        _description = new RichTextLabel
        {
            Modulate = LogicCircuitStyle.TextDim,
            Margin = new Thickness(0f, 0f, 0f, 6f),
        };

        _fields = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
        };

        _delete = new Button
        {
            Text = Loc.GetString("logic-circuit-inspector-delete"),
            Visible = false,
        };

        _delete.OnPressed += _ =>
        {
            if (_node != null)
                OnDeleteRequested?.Invoke(_node);
        };

        AddChild(_title);
        AddChild(_description);

        AddChild(new ScrollContainer
        {
            VerticalExpand = true,
            HScrollEnabled = false,
            Children = { _fields },
        });

        AddChild(_delete);
    }

    public void SetPortCount(int count)
    {
        _portCount = Math.Max(1, count);
    }

    public void Show(LogicNodeControl? node)
    {
        _node = node;
        _fields.RemoveAllChildren();
        _delete.Visible = node != null;

        if (node == null)
        {
            _title.Text = Loc.GetString("logic-circuit-inspector-empty");
            _description.SetMessage(string.Empty);
            return;
        }

        _title.Text = $"{Loc.GetString(node.Proto.Name)} ({node.Node.Id})";
        _description.SetMessage(node.Proto.Description == null
            ? string.Empty
            : Loc.GetString(node.Proto.Description.Value));

        for (var i = 0; i < node.Proto.Config.Count && i < node.Node.Config.Count; i++)
        {
            _fields.AddChild(MakeField(node, i));
        }
    }

    private Control MakeField(LogicNodeControl node, int index)
    {
        var field = node.Proto.Config[index];

        var box = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Margin = new Thickness(0f, 0f, 0f, 6f),
        };

        box.AddChild(new Label
        {
            Text = Loc.GetString(field.Name),
            Modulate = LogicCircuitStyle.TextDim,
        });

        box.AddChild(field.Type switch
        {
            LogicConfigFieldType.Boolean => MakeBoolean(node, index),
            LogicConfigFieldType.DevicePort => MakePort(node, index, field),
            _ => MakeTextOrNumber(node, index, field),
        });

        return box;
    }

    private Control MakeBoolean(LogicNodeControl node, int index)
    {
        var check = new CheckBox
        {
            Text = Loc.GetString("logic-circuit-inspector-enabled"),
            Pressed = node.Node.Config[index].AsBool(),
        };

        check.OnToggled += args =>
        {
            node.Node.Config[index] = LogicSignal.FromBool(args.Pressed);
            OnConfigChanged?.Invoke();
        };

        return check;
    }

    private Control MakePort(LogicNodeControl node, int index, LogicElementConfigField field)
    {
        var option = new OptionButton { HorizontalExpand = true };
        var limit = Math.Min(_portCount, (int) MathF.Max(1f, field.Max));

        for (var port = 1; port <= limit; port++)
        {
            option.AddItem(port.ToString(), port);
        }

        var current = (int) node.Node.Config[index].AsNumber();

        if (current >= 1 && current <= limit)
            option.SelectId(current);

        option.OnItemSelected += args =>
        {
            option.SelectId(args.Id);
            node.Node.Config[index] = LogicSignal.FromNumber(args.Id);
            OnConfigChanged?.Invoke();
        };

        return option;
    }

    private Control MakeTextOrNumber(LogicNodeControl node, int index, LogicElementConfigField field)
    {
        var edit = new LineEdit
        {
            Text = node.Node.Config[index].AsText(),
            HorizontalExpand = true,
        };

        edit.OnTextChanged += args =>
        {
            if (field.Type == LogicConfigFieldType.Number)
            {
                if (args.Text.Length == 0)
                {
                    node.Node.Config[index] = LogicSignal.FromNumber(0f);
                    OnConfigChanged?.Invoke();
                    return;
                }

                if (!float.TryParse(args.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                    return;

                node.Node.Config[index] = LogicSignal.FromNumber(Math.Clamp(number, field.Min, field.Max));
                OnConfigChanged?.Invoke();
                return;
            }

            node.Node.Config[index] = LogicSignal.FromText(args.Text);
            OnConfigChanged?.Invoke();
        };

        return edit;
    }
}
