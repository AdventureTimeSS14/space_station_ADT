using Content.Shared.ADT.InconnuOS;
using Content.Shared.DeviceLinking;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.InconnuOS.UI.Apps;

public sealed class DeviceMonitorApp : OsAppControl
{
    private readonly BoxContainer _inputs;
    private readonly BoxContainer _outputs;
    private readonly Label _empty;

    private readonly List<OsInfoRow> _inputRows = new();
    private readonly List<OsInfoRow> _outputRows = new();

    private bool _subscribed;

    public DeviceMonitorApp()
    {
        _inputs = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
        };

        _outputs = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
        };

        _empty = new Label
        {
            Text = Loc.GetString("os-devices-waiting"),
            Modulate = OsStyle.TextDim,
            Margin = new Thickness(10f, 4f, 10f, 4f),
        };

        var content = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children =
            {
                _empty,
                OsWidgets.Caption(Loc.GetString("os-devices-inputs")),
                _inputs,
                OsWidgets.Caption(Loc.GetString("os-devices-outputs")),
                _outputs,
            },
        };

        AddChild(new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Margin = new Thickness(4f),
            Children = { content },
        });
    }

    public override void OnOpen(string? argument)
    {
        base.OnOpen(argument);

        if (_subscribed)
            return;

        _subscribed = true;

        Context.MessageReceived += OnServerMessage;
        Context.Send(new ADTOsRequestCircuitMessage());
    }

    private void OnServerMessage(BoundUserInterfaceMessage message)
    {
        if (message is not ADTOsPortsMessage ports)
            return;

        _empty.Visible = ports.Inputs.Length == 0 && ports.Outputs.Length == 0;

        Fill(_inputs, _inputRows, ports.Inputs, true);
        Fill(_outputs, _outputRows, ports.Outputs, false);
    }

    private void Fill(BoxContainer container, List<OsInfoRow> rows, OsPortInfo[] ports, bool input)
    {
        while (rows.Count < ports.Length)
        {
            var row = new OsInfoRow(string.Empty, string.Empty);

            rows.Add(row);
            container.AddChild(row);
        }

        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].Visible = i < ports.Length;
        }

        for (var i = 0; i < ports.Length; i++)
        {
            var port = ports[i];

            var value = port.Value.AsText();

            if (value.Length == 0)
                value = Loc.GetString("os-devices-empty");

            rows[i].Caption = GetPortName(port.Port, input);

            rows[i].Value = port.Links < 0
                ? value
                : Loc.GetString("os-devices-value", ("value", value), ("links", port.Links));
        }
    }

    private string GetPortName(string id, bool input)
    {
        if (input && Context.Prototypes.TryIndex<SinkPortPrototype>(id, out var sink))
            return Loc.GetString(sink.Name);

        if (!input && Context.Prototypes.TryIndex<SourcePortPrototype>(id, out var source))
            return Loc.GetString(source.Name);

        return id;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing || !_subscribed)
            return;

        Context.MessageReceived -= OnServerMessage;
    }
}
