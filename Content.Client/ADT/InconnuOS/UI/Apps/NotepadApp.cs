using Content.Shared.ADT.InconnuOS;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.ADT.InconnuOS.UI.Apps;

public sealed class NotepadApp : OsAppControl
{
    private readonly TextEdit _text;
    private readonly LineEdit _prompt;
    private readonly Label _status;
    private readonly Button _save;

    private string? _path;
    private string _saved = string.Empty;
    private bool _dirty;
    private bool _closeAsked;

    public NotepadApp()
    {
        _text = new TextEdit
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Margin = new Thickness(4f, 2f, 4f, 2f),
        };

        _save = OsWidgets.Small(Loc.GetString("os-notepad-save"));

        var saveAs = OsWidgets.Small(Loc.GetString("os-notepad-save-as"));

        _status = new Label
        {
            Modulate = OsStyle.TextDim,
            HorizontalExpand = true,
            ClipText = true,
        };

        _prompt = new LineEdit
        {
            HorizontalExpand = true,
            Visible = false,
            Margin = new Thickness(4f, 0f, 4f, 0f),
            PlaceHolder = Loc.GetString("os-notepad-prompt"),
        };

        var toolbar = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new Thickness(4f, 4f, 4f, 2f),
            Children = { _save, saveAs, _status },
        };

        AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
            Children = { toolbar, _prompt, _text },
        });

        _text.OnTextChanged += _ => OnEdited();
        _save.OnPressed += _ => Save();
        saveAs.OnPressed += _ => OpenPrompt();
        _prompt.OnTextEntered += _ => ConfirmPrompt();
    }

    public override void OnOpen(string? argument)
    {
        base.OnOpen(argument);

        if (argument == null)
        {
            SetText(string.Empty);
            Refresh();
            return;
        }

        _path = argument;

        SetText(Context.FindFile(argument)?.Text ?? string.Empty);
        Refresh();
    }

    public override void OnStateChanged()
    {
        base.OnStateChanged();

        if (!_dirty && _path != null && Context.FindFile(_path) is { } file && file.Text != _saved)
            SetText(file.Text);

        Refresh();
    }

    private sealed record NotepadState(string? Path, string Text, string Saved);

    public override object? SaveState()
    {
        return new NotepadState(_path, Current(), _saved);
    }

    public override void LoadState(object state)
    {
        if (state is not NotepadState saved)
            return;

        _path = saved.Path;
        _saved = saved.Saved;
        _text.TextRope = new Rope.Leaf(saved.Text);
        _dirty = saved.Text != saved.Saved;

        Refresh();
    }

    public override bool OnClosing()
    {
        if (!_dirty || _closeAsked)
            return true;

        _closeAsked = true;

        Context.Toast(Loc.GetString("os-notepad-unsaved"));

        return false;
    }

    private void SetText(string text)
    {
        _saved = text;
        _dirty = false;

        _text.TextRope = new Rope.Leaf(text);
    }

    private void OnEdited()
    {
        _dirty = Current() != _saved;
        _closeAsked = false;

        Refresh();
    }

    private string Current()
    {
        return Rope.Collapse(_text.TextRope);
    }

    private void Save()
    {
        if (_path == null)
        {
            OpenPrompt();
            return;
        }

        var text = Current();

        if (text.Length > Context.State.Limits.MaxFileLength)
        {
            Context.Toast(OsErrors.GetMessage(OsValidationError.FileTooLong,
                Context.State.Limits.MaxFileLength.ToString()));
            return;
        }

        Context.Send(new ADTOsFileWriteMessage(_path, OsPath.KindFromExtension(_path), text, null));

        _saved = text;
        _dirty = false;

        Refresh();
    }

    private void OpenPrompt()
    {
        _prompt.Visible = true;
        _prompt.Text = _path == null ? string.Empty : OsPath.GetName(_path);

        _prompt.GrabKeyboardFocus();
    }

    private void ConfirmPrompt()
    {
        var name = _prompt.Text.Trim();

        if (!name.Contains('.'))
            name += ".txt";

        if (!OsPath.IsValidName(name))
        {
            Context.Toast(OsErrors.GetMessage(OsValidationError.InvalidPath, name));
            return;
        }

        var folder = _path == null ? WritableRoot() : OsPath.GetParent(_path);

        _path = OsPath.Combine(folder, name);

        _prompt.Visible = false;
        _prompt.Text = string.Empty;

        Save();
    }

    private string WritableRoot()
    {
        foreach (var drive in Context.State.Drives)
        {
            if (!drive.ReadOnly)
                return OsPath.GetRoot(drive.Letter);
        }

        return "C:/";
    }

    private void Refresh()
    {
        var name = _path == null
            ? Loc.GetString("os-notepad-new")
            : OsPath.GetName(_path);

        SetTitle(_dirty ? $"{name} *" : name);

        var file = _path == null ? null : Context.FindFile(_path);
        var writable = _path == null || Context.IsWritable(_path);

        _save.Disabled = !writable || file is { ReadOnly: true };

        _status.Text = Loc.GetString("os-notepad-status",
            ("length", Current().Length),
            ("limit", Context.State.Limits.MaxFileLength));
    }
}
