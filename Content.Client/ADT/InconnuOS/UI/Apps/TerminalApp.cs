using System.Numerics;
using Content.Shared.ADT.InconnuOS;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.ADT.InconnuOS.UI.Apps;

public sealed class TerminalApp : OsAppControl
{
    private const int MaxLines = 400;

    private readonly BoxContainer _output;
    private readonly ScrollContainer _scroll;
    private readonly HistoryLineEdit _input;
    private readonly Label _prompt;

    private Font? _mono;

    private string _directory = "C:/";
    private bool _started;

    public TerminalApp()
    {
        _output = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };

        _scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Margin = new Thickness(6f, 6f, 6f, 2f),
            Children = { _output },
        };

        _prompt = new Label
        {
            Text = _directory + ">",
            Modulate = OsStyle.Good,
            Margin = new Thickness(6f, 0f, 4f, 0f),
            VerticalAlignment = VAlignment.Center,
        };

        _input = new HistoryLineEdit
        {
            HorizontalExpand = true,
            Margin = new Thickness(0f, 0f, 6f, 6f),
        };

        AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
            Children =
            {
                _scroll,
                new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    Children = { _prompt, _input },
                },
            },
        });

        _input.OnTextEntered += OnEntered;
    }

    public override void OnOpen(string? argument)
    {
        base.OnOpen(argument);

        if (_started)
            return;

        _started = true;

        _mono = OsStyle.CreateMonoFont(Context.Cache, 11);
        _prompt.FontOverride = _mono;

        Context.MessageReceived += OnServerMessage;

        Print(OsBrand.FullVersion, OsStyle.TextBright);
        Print(OsBrand.Copyright, OsStyle.TextDim);
        Print(string.Empty, OsStyle.Text);
        Print(Loc.GetString("os-terminal-greeting"), OsStyle.TextDim);
        Print(string.Empty, OsStyle.Text);

        _input.GrabKeyboardFocus();
    }

    private sealed record TerminalState(List<(string Text, Color Color)> Lines, string Directory, List<string> History);

    public override object? SaveState()
    {
        var lines = new List<(string, Color)>();

        foreach (var child in _output.Children)
        {
            if (child is Label label)
                lines.Add((label.Text ?? string.Empty, label.Modulate));
        }

        return new TerminalState(lines, _directory, new List<string>(_input.History));
    }

    public override void LoadState(object state)
    {
        if (state is not TerminalState saved)
            return;

        _output.RemoveAllChildren();

        foreach (var (text, color) in saved.Lines)
        {
            Print(text, color);
        }

        _directory = saved.Directory;
        _prompt.Text = _directory + ">";

        _input.History.Clear();
        _input.History.AddRange(saved.History);
        _input.HistoryIndex = _input.History.Count;
    }

    private void OnEntered(LineEdit.LineEditEventArgs args)
    {
        var line = args.Text;

        _input.Clear();
        _input.GrabKeyboardFocus();

        Print($"{_directory}>{line}", OsStyle.Text);

        if (line.Trim().Length == 0)
            return;

        Context.Send(new ADTOsCommandMessage(line));
    }

    private void OnServerMessage(BoundUserInterfaceMessage message)
    {
        if (message is not ADTOsOutputMessage output)
            return;

        _directory = output.WorkingDirectory;
        _prompt.Text = _directory + ">";

        foreach (var line in output.Lines)
        {
            Print(line, OsStyle.Text);
        }

        Print(string.Empty, OsStyle.Text);
    }

    private void Print(string text, Color color)
    {
        _output.AddChild(new Label
        {
            Text = text,
            FontOverride = _mono,
            Modulate = color,
        });

        while (_output.ChildCount > MaxLines)
        {
            _output.RemoveChild(_output.GetChild(0));
        }

        _scroll.SetScrollValue(new Vector2(0f, float.MaxValue));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        Context.MessageReceived -= OnServerMessage;
    }
}
