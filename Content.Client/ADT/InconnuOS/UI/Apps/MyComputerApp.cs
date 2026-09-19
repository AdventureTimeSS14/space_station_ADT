using Content.Shared.ADT.InconnuOS;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.ADT.InconnuOS.UI.Apps;

public sealed class MyComputerApp : OsAppControl
{
    private readonly BoxContainer _drives;

    private readonly OsInfoRow _os;
    private readonly OsInfoRow _publisher;
    private readonly OsInfoRow _licence;
    private readonly OsInfoRow _machine;
    private readonly OsInfoRow _user;
    private readonly OsInfoRow _uptime;

    public MyComputerApp()
    {
        _os = new OsInfoRow(Loc.GetString("os-mycomputer-os"), OsBrand.FullVersion);
        _publisher = new OsInfoRow(Loc.GetString("os-mycomputer-publisher"), OsBrand.Publisher);
        _licence = new OsInfoRow(Loc.GetString("os-mycomputer-licence"), string.Empty);
        _machine = new OsInfoRow(Loc.GetString("os-mycomputer-machine"), string.Empty);
        _user = new OsInfoRow(Loc.GetString("os-mycomputer-user"), string.Empty);
        _uptime = new OsInfoRow(Loc.GetString("os-mycomputer-uptime"), string.Empty);

        _drives = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(0f, 0f, 0f, 8f),
        };

        var system = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children =
            {
                OsWidgets.Caption(Loc.GetString("os-mycomputer-system")),
                _os,
                _publisher,
                _licence,
                _machine,
                _user,
                _uptime,
            },
        };

        var content = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Children =
            {
                system,
                OsWidgets.Caption(Loc.GetString("os-mycomputer-drives")),
                _drives,
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

        Refresh();
    }

    public override void OnStateChanged()
    {
        base.OnStateChanged();

        Refresh();
    }

    private void Refresh()
    {
        var state = Context.State;

        _licence.Value = state.Activated
            ? Loc.GetString("os-mycomputer-licence-ok")
            : Loc.GetString("os-mycomputer-licence-no");

        _machine.Value = state.MachineName;
        _user.Value = state.UserName;

        _drives.RemoveAllChildren();

        foreach (var drive in state.Drives)
        {
            _drives.AddChild(BuildDrive(drive));
        }
    }

    private Control BuildDrive(OsDriveState drive)
    {
        var used = drive.Disk.TotalSize;

        var bar = new OsUsageBar(Context.Accent)
        {
            Fraction = drive.Capacity <= 0 ? 0f : used / (float) drive.Capacity,
            HorizontalExpand = true,
            Margin = new Thickness(10f, 2f, 10f, 6f),
        };

        var title = Loc.GetString("os-mycomputer-drive",
            ("letter", drive.Letter),
            ("label", drive.Label));

        var trailing = drive.ReadOnly
            ? Loc.GetString("os-mycomputer-drive-locked")
            : null;

        var row = new OsListRow(drive.Removable ? OsAppIcon.Disk : OsAppIcon.Computer, title, trailing, Context.Accent);

        row.OnSelected += () => OpenDrive(drive);
        row.OnActivated += () => OpenDrive(drive);

        var free = Math.Max(0, drive.Capacity - used);

        var usage = new OsInfoRow(Loc.GetString("os-mycomputer-drive-usage"),
            Loc.GetString("os-mycomputer-drive-usage-value",
                ("free", OsFormat.Size(free)),
                ("total", OsFormat.Size(drive.Capacity))));

        return new OsPanel
        {
            Margin = new Thickness(4f, 2f, 4f, 2f),
            Children =
            {
                new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    Children =
                    {
                        row,
                        usage,
                        bar,
                    },
                },
            },
        };
    }

    private void OpenDrive(OsDriveState drive)
    {
        Context.OpenApp("OsAppExplorer", OsPath.GetRoot(drive.Letter));
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _uptime.Value = OsFormat.Time(Context.Uptime);
    }
}
