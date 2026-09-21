using System.Numerics;
using Content.Shared.ADT.InconnuOS;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.ADT.InconnuOS.UI.Apps;

public sealed class ExplorerApp : OsAppControl
{
    private const long SlowDeleteSize = 1024L * 1024 * 1024;
    private const float SlowDeleteSeconds = 15f;

    private readonly BoxContainer _drives;
    private readonly BoxContainer _files;
    private readonly LineEdit _address;
    private readonly LineEdit _prompt;
    private readonly Label _status;

    private readonly OsPanel _deletePanel;
    private readonly Label _deleteTitle;
    private readonly Label _deleteProgress;
    private readonly OsUsageBar _deleteBar;

    private readonly Button _up;
    private readonly Button _back;

    private readonly List<string> _history = new();

    private string _path = string.Empty;
    private string? _selected;
    private string? _renaming;

    private string? _deleting;
    private long _deletingSize;
    private float _deletingElapsed;

    public ExplorerApp()
    {
        _address = new LineEdit
        {
            HorizontalExpand = true,
            Margin = new Thickness(4f, 0f, 0f, 0f),
        };

        _back = OsWidgets.Small(Loc.GetString("os-explorer-back"));
        _up = OsWidgets.Small(Loc.GetString("os-explorer-up"));

        var toolbar = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new Thickness(6f, 6f, 6f, 4f),
            Children = { _back, _up, _address },
        };

        _drives = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            MinWidth = 150f,
            Margin = new Thickness(2f),
        };

        _files = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Margin = new Thickness(2f),
        };

        var area = new OsClickArea
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Children = { _files },
        };

        var middle = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            VerticalExpand = true,
            Margin = new Thickness(6f, 0f, 6f, 0f),
            Children =
            {
                new OsPanel
                {
                    Children = { _drives },
                },
                new OsPanel
                {
                    HorizontalExpand = true,
                    Margin = new Thickness(6f, 0f, 0f, 0f),
                    Children =
                    {
                        new ScrollContainer
                        {
                            HorizontalExpand = true,
                            VerticalExpand = true,
                            Margin = new Thickness(1f),
                            Children = { area },
                        },
                    },
                },
            },
        };

        _prompt = new LineEdit
        {
            HorizontalExpand = true,
            Visible = false,
            PlaceHolder = Loc.GetString("os-explorer-prompt-name"),
            Margin = new Thickness(6f, 4f, 6f, 0f),
        };

        _status = new Label
        {
            Modulate = OsStyle.TextDim,
            ClipText = true,
            Margin = new Thickness(10f, 4f, 10f, 6f),
        };

        _deleteTitle = new Label
        {
            Modulate = OsStyle.Text,
            ClipText = true,
        };

        _deleteProgress = new Label
        {
            Modulate = OsStyle.TextDim,
            ClipText = true,
            HorizontalExpand = true,
            VerticalAlignment = VAlignment.Center,
        };

        _deleteBar = new OsUsageBar(OsStyle.Error)
        {
            HorizontalExpand = true,
            Margin = new Thickness(0f, 4f, 0f, 4f),
        };

        var cancel = OsWidgets.Small(Loc.GetString("os-explorer-deleting-cancel"));

        cancel.OnPressed += _ => CancelDelete();

        _deletePanel = new OsPanel
        {
            Visible = false,
            Margin = new Thickness(6f, 4f, 6f, 0f),
            Children =
            {
                new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    Margin = new Thickness(10f, 6f, 6f, 6f),
                    Children =
                    {
                        _deleteTitle,
                        _deleteBar,
                        new BoxContainer
                        {
                            Orientation = BoxContainer.LayoutOrientation.Horizontal,
                            Children = { _deleteProgress, cancel },
                        },
                    },
                },
            },
        };

        AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
            Children = { toolbar, middle, _deletePanel, _prompt, _status },
        });

        _address.OnTextEntered += args => Navigate(args.Text, true);
        _prompt.OnTextEntered += _ => ConfirmRename();

        _back.OnPressed += _ => GoBack();
        _up.OnPressed += _ => Navigate(OsPath.GetParent(_path), true);

        area.OnContextMenu += ShowFolderMenu;
        area.OnClicked += ClearSelection;
    }

    public override void OnOpen(string? argument)
    {
        base.OnOpen(argument);

        var start = argument ?? FirstDriveRoot();
        var select = (string?) null;

        if (Context.FindFile(start) is { IsDirectory: false })
        {
            select = start;
            start = OsPath.GetParent(start);
        }

        Navigate(start, false);

        if (select == null)
            return;

        _selected = select;
        Refresh();
    }

    public override void OnStateChanged()
    {
        base.OnStateChanged();

        Refresh();
    }

    public override object? SaveState()
    {
        return _path;
    }

    public override void LoadState(object state)
    {
        if (state is string path)
            Navigate(path, false);
    }

    private string FirstDriveRoot()
    {
        var drives = Context.State.Drives;

        if (drives.Length == 0)
            return "C:/";

        return OsPath.GetRoot(drives[0].Letter);
    }

    private void Navigate(string path, bool remember)
    {
        if (!OsPath.TryNormalize(path, out var full))
        {
            Context.Toast(OsErrors.GetMessage(OsValidationError.InvalidPath, path));
            return;
        }

        if (Context.GetDriveOf(full) == null)
        {
            Context.Toast(OsErrors.GetMessage(OsValidationError.UnknownDrive, OsPath.GetDriveLetter(full).ToString()));
            return;
        }

        if (remember && _path.Length > 0 && _path != full)
            _history.Add(_path);

        _path = full;
        _selected = null;

        CloseRename();
        Refresh();
    }

    private void GoBack()
    {
        if (_history.Count == 0)
            return;

        var last = _history[^1];

        _history.RemoveAt(_history.Count - 1);

        Navigate(last, false);
    }

    private void Refresh()
    {
        if (Context.GetDriveOf(_path) == null)
            _path = FirstDriveRoot();

        _address.Text = _path;

        RefreshDrives();
        RefreshFiles();

        _up.Disabled = OsPath.IsRoot(_path);
        _back.Disabled = _history.Count == 0;

        SetTitle(Loc.GetString("os-explorer-title", ("path", _path)));
    }

    private void RefreshDrives()
    {
        _drives.RemoveAllChildren();

        foreach (var drive in Context.State.Drives)
        {
            var root = OsPath.GetRoot(drive.Letter);

            var row = new OsListRow(
                drive.Removable ? OsAppIcon.Disk : OsAppIcon.Computer,
                Loc.GetString("os-explorer-drive", ("letter", drive.Letter), ("label", drive.Label)),
                null,
                Context.Accent)
            {
                Selected = OsPath.GetDriveLetter(_path) == char.ToUpperInvariant(drive.Letter),
                ClickKey = root,
            };

            row.OnSelected += () => Navigate(root, true);
            row.OnActivated += () => Navigate(root, true);

            _drives.AddChild(row);
        }
    }

    private void RefreshFiles()
    {
        _files.RemoveAllChildren();

        var files = Context.List(_path);

        foreach (var file in files)
        {
            var entry = file;

            var trailing = file.IsDirectory
                ? null
                : OsFormat.Size(file.ShownSize);

            var row = new OsListRow(OsWidgets.IconFor(file), file.Name, trailing, Context.Accent)
            {
                Selected = _selected != null && _selected.Equals(file.Path, OsPath.Comparison),
                ClickKey = file.Path,
            };

            row.OnSelected += () => Select(entry.Path);
            row.OnActivated += () => Activate(entry);
            row.OnContextMenu += screen => ShowFileMenu(entry, screen);

            _files.AddChild(row);
        }

        var drive = Context.GetDriveOf(_path);
        var free = drive == null ? 0L : Math.Max(0L, drive.Capacity - drive.Disk.TotalSize);

        _status.Text = Loc.GetString("os-explorer-status",
            ("count", files.Count),
            ("free", OsFormat.Size(free)));
    }

    private void Select(string path)
    {
        _selected = path;

        Refresh();
    }

    private void ClearSelection()
    {
        if (_selected == null)
            return;

        _selected = null;

        Refresh();
    }

    private void Activate(OsFile file)
    {
        if (file.IsDirectory)
        {
            Navigate(file.Path, true);
            return;
        }

        Context.OpenFile(file.Path);
    }

    private bool CanEdit(OsFile file)
    {
        return Context.IsWritable(file.Path) && !file.ReadOnly;
    }

    private void ShowFileMenu(OsFile file, Vector2 screen)
    {
        var editable = CanEdit(file);

        var entries = new List<OsMenuEntry>
        {
            new(Loc.GetString("os-explorer-menu-open"), () => Activate(file)),
            OsMenuEntry.Line(),
            new(Loc.GetString("os-explorer-menu-rename"), () => BeginRename(file.Path), !editable),
            new(Loc.GetString("os-explorer-menu-delete"), () => Delete(file.Path), !editable),
        };

        var targets = CopyTargets(file.Path);

        if (targets.Count > 0)
            entries.Add(OsMenuEntry.Line());

        foreach (var target in targets)
        {
            var letter = target.Letter;

            entries.Add(new OsMenuEntry(
                Loc.GetString("os-explorer-menu-copy", ("letter", letter), ("label", target.Label)),
                () => CopyTo(file.Path, letter)));
        }

        entries.Add(OsMenuEntry.Line());
        entries.Add(new OsMenuEntry(Loc.GetString("os-explorer-menu-properties"), () => ShowProperties(file)));

        Context.ShowMenu(screen, entries);
    }

    private void ShowFolderMenu(Vector2 screen)
    {
        var writable = Context.IsWritable(_path);

        var entries = new List<OsMenuEntry>
        {
            new(Loc.GetString("os-explorer-menu-new-folder"), CreateFolder, !writable),
            new(Loc.GetString("os-explorer-menu-new-text"), CreateText, !writable),
            OsMenuEntry.Line(),
            new(Loc.GetString("os-explorer-menu-refresh"), Refresh),
        };

        Context.ShowMenu(screen, entries);
    }

    private List<OsDriveState> CopyTargets(string path)
    {
        var result = new List<OsDriveState>();
        var letter = OsPath.GetDriveLetter(path);

        foreach (var drive in Context.State.Drives)
        {
            if (char.ToUpperInvariant(drive.Letter) == letter || drive.ReadOnly)
                continue;

            result.Add(drive);
        }

        return result;
    }

    private string UniqueName(string baseName, string extension)
    {
        for (var i = 1; ; i++)
        {
            var name = i == 1
                ? baseName + extension
                : $"{baseName} ({i}){extension}";

            var path = OsPath.Combine(_path, name);

            if (Context.FindFile(path) == null && !HasImplicitFolder(path))
                return name;
        }
    }

    private bool HasImplicitFolder(string path)
    {
        foreach (var file in Context.List(_path))
        {
            if (file.Path.Equals(path, OsPath.Comparison))
                return true;
        }

        return false;
    }

    private void CreateFolder()
    {
        var name = UniqueName(Loc.GetString("os-explorer-new-folder-name"), string.Empty);
        var path = OsPath.Combine(_path, name);

        Context.Send(new ADTOsCreateDirectoryMessage(path));

        _selected = path;
        BeginRename(path);
    }

    private void CreateText()
    {
        var name = UniqueName(Loc.GetString("os-explorer-new-text-name"), OsPath.ExtensionForKind(OsFileKind.Text));
        var path = OsPath.Combine(_path, name);

        Context.Send(new ADTOsFileWriteMessage(path, OsFileKind.Text, string.Empty, null));

        _selected = path;
        BeginRename(path);
    }

    private void BeginRename(string path)
    {
        _renaming = path;

        _prompt.Visible = true;
        _prompt.Text = OsPath.GetName(path);

        _prompt.GrabKeyboardFocus();
    }

    private void CloseRename()
    {
        _renaming = null;

        _prompt.Visible = false;
        _prompt.Text = string.Empty;
    }

    private void ConfirmRename()
    {
        if (_renaming == null)
            return;

        var name = _prompt.Text.Trim();

        if (name == OsPath.GetName(_renaming))
        {
            CloseRename();
            return;
        }

        if (!OsPath.IsValidName(name))
        {
            Context.Toast(OsErrors.GetMessage(OsValidationError.InvalidPath, name));
            return;
        }

        var target = OsPath.Combine(OsPath.GetParent(_renaming), name);

        Context.Send(new ADTOsFileMoveMessage(_renaming, target, false));

        _selected = target;
        CloseRename();
    }

    private void Delete(string path)
    {
        var size = MeasureDelete(path);

        if (size < SlowDeleteSize)
        {
            SendDelete(path);
            return;
        }

        if (_deleting != null)
        {
            Context.Toast(Loc.GetString("os-explorer-deleting-busy"));
            return;
        }

        _deleting = path;
        _deletingSize = size;
        _deletingElapsed = 0f;

        _deleteTitle.Text = Loc.GetString("os-explorer-deleting-title", ("name", OsPath.GetName(path)));
        _deletePanel.Visible = true;

        UpdateDeleteProgress();
    }

    private long MeasureDelete(string path)
    {
        if (Context.GetDriveOf(path) is not { } drive)
            return 0L;

        var size = 0L;

        foreach (var file in drive.Disk.Files)
        {
            if (file.Path.Equals(path, OsPath.Comparison) || OsPath.IsInside(file.Path, path))
                size += file.ShownSize;
        }

        return size;
    }

    private void SendDelete(string path)
    {
        Context.Send(new ADTOsFileDeleteMessage(path));

        if (_selected == path)
            _selected = null;
    }

    private void CancelDelete()
    {
        if (_deleting == null)
            return;

        Context.Toast(Loc.GetString("os-explorer-deleting-cancelled", ("name", OsPath.GetName(_deleting))));

        StopDelete();
    }

    private void StopDelete()
    {
        _deleting = null;
        _deletingSize = 0L;
        _deletingElapsed = 0f;

        _deletePanel.Visible = false;
    }

    private void UpdateDeleteProgress()
    {
        var fraction = Math.Clamp(_deletingElapsed / SlowDeleteSeconds, 0f, 1f);
        var remaining = (int) MathF.Ceiling(SlowDeleteSeconds - _deletingElapsed);

        _deleteBar.Fraction = fraction;
        _deleteProgress.Text = Loc.GetString("os-explorer-deleting-progress",
            ("done", OsFormat.Size((long) (_deletingSize * (double) fraction))),
            ("total", OsFormat.Size(_deletingSize)),
            ("seconds", Math.Max(0, remaining)));
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_deleting == null)
            return;

        _deletingElapsed += args.DeltaSeconds;

        if (_deletingElapsed < SlowDeleteSeconds)
        {
            UpdateDeleteProgress();
            return;
        }

        var path = _deleting;

        StopDelete();
        SendDelete(path);
    }

    private void CopyTo(string path, char letter)
    {
        var target = OsPath.Combine(OsPath.GetRoot(letter), OsPath.GetName(path));

        Context.Send(new ADTOsFileMoveMessage(path, target, true));
    }

    private void ShowProperties(OsFile file)
    {
        var kind = file.IsDirectory
            ? Loc.GetString("os-explorer-kind-folder")
            : Loc.GetString($"os-explorer-kind-{file.Kind.ToString().ToLowerInvariant()}");

        Context.Toast(Loc.GetString("os-explorer-properties",
            ("name", file.Name),
            ("kind", kind),
            ("size", OsFormat.Size(file.ShownSize))));
    }
}
