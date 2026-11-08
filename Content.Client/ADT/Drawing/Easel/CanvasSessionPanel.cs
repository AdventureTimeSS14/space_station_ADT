using System.Linq;
using System.Numerics;
using Content.Client.ADT.Drawing.Sync;
using Content.Client.ADT.Drawing.UI;
using Content.Client.UserInterface.Controls;
using Content.Shared.ADT.Drawing;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Drawing.Easel;

public sealed class CanvasSessionPanel : BoxContainer
{
    private readonly BoxContainer _participants;
    private readonly BoxContainer _requests;
    private readonly BoxContainer _actions;
    private readonly RichTextLabel _hint;

    public CanvasSessionPanel()
    {
        Orientation = LayoutOrientation.Vertical;
        SeparationOverride = 4;

        AddChild(new Label
        {
            Text = Loc.GetString("adt-canvas-session-header"),
            StyleClasses = { "LabelHeading" },
        });

        _participants = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 2,
        };
        _requests = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 2,
        };
        _actions = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 4,
        };
        _hint = new RichTextLabel
        {
            HorizontalExpand = true,
            Visible = false,
        };

        AddChild(_participants);
        AddChild(_requests);
        AddChild(_actions);
        AddChild(_hint);
    }

    public event Action? JoinRequested;
    public event Action? JoinCancelled;
    public event Action<NetUserId, bool>? JoinDecided;
    public event Action<NetUserId>? InviteRequested;
    public event Action<bool>? InviteAnswered;
    public event Action<NetUserId>? KickRequested;
    public event Action<NetUserId>? HostTransferRequested;
    public event Action? LeaveRequested;
    public event Action? RevertRequested;

    public void ShowHint(string? text)
    {
        _hint.SetMessage(FormattedMessage.FromUnformatted(text ?? string.Empty), Color.Gray);
        _hint.Visible = !string.IsNullOrEmpty(text);
    }

    public void Refresh(CanvasEaselBoundUserInterfaceState state, NetUserId? localUser)
    {
        _participants.RemoveAllChildren();
        _requests.RemoveAllChildren();
        _actions.RemoveAllChildren();

        var local = state.Participants.FirstOrDefault(p => p.User == localUser);
        var localRole = local?.Role ?? CanvasRole.Viewer;
        var host = localRole == CanvasRole.Host;
        var editors = state.Participants.Count(p => p.Role != CanvasRole.Viewer);
        var full = editors >= state.MaxEditors;

        foreach (var participant in state.Participants)
        {
            _participants.AddChild(ParticipantRow(participant, participant.User == localUser, host, full));
        }

        if (host)
        {
            foreach (var request in state.Requests)
            {
                if (request.Invite)
                    continue;

                var user = request.User;
                _requests.AddChild(DrawingWindow.WrapLabel(
                    Loc.GetString("adt-canvas-session-request", ("name", request.Name)), Color.Gold));

                var accept = WideButton("adt-canvas-session-accept", () => JoinDecided?.Invoke(user, true));
                accept.Disabled = full;
                var reject = WideButton("adt-canvas-session-reject", () => JoinDecided?.Invoke(user, false));
                _requests.AddChild(Row(accept, reject));
            }

            var revert = new ConfirmButton
            {
                Text = Loc.GetString("adt-canvas-session-revert"),
                ConfirmationText = Loc.GetString("adt-canvas-session-confirm"),
                HorizontalExpand = true,
            };
            revert.OnPressed += _ => RevertRequested?.Invoke();
            _actions.AddChild(revert);
        }

        if (localRole == CanvasRole.Editor)
            _actions.AddChild(WideButton("adt-canvas-session-leave", () => LeaveRequested?.Invoke()));

        if (localRole == CanvasRole.Viewer && localUser is { } me)
        {
            var request = state.Requests.FirstOrDefault(r => r.User == me);
            if (request is { Invite: true })
            {
                _actions.AddChild(DrawingWindow.WrapLabel(
                    Loc.GetString("adt-canvas-session-invited", ("name", state.HostName ?? "?")), Color.Gold));
                _actions.AddChild(Row(
                    WideButton("adt-canvas-session-accept", () => InviteAnswered?.Invoke(true)),
                    WideButton("adt-canvas-session-decline", () => InviteAnswered?.Invoke(false))));
            }
            else if (request != null)
            {
                _actions.AddChild(DrawingWindow.WrapLabel(Loc.GetString("adt-canvas-session-request-sent"), Color.Gray));
                _actions.AddChild(WideButton("adt-canvas-session-cancel", () => JoinCancelled?.Invoke()));
            }
            else
            {
                var join = WideButton("adt-canvas-session-join", () => JoinRequested?.Invoke());
                join.Disabled = full;
                if (full)
                    join.ToolTip = Loc.GetString("adt-canvas-join-full");

                _actions.AddChild(join);
            }
        }
    }

    private Control ParticipantRow(CanvasParticipantInfo participant, bool self, bool host, bool full)
    {
        var swatch = new PanelContainer
        {
            MinSize = new Vector2(12, 12),
            VerticalAlignment = VAlignment.Center,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = CanvasCollaboration.ColorOf(participant.Index),
            },
        };

        var role = Loc.GetString($"adt-canvas-role-{participant.Role.ToString().ToLowerInvariant()}");
        var text = Loc.GetString("adt-canvas-session-participant",
            ("name", participant.Name),
            ("role", role),
            ("self", self ? 1 : 0),
            ("away", participant.Present ? 0 : 1));

        var label = new Label
        {
            Text = text,
            HorizontalExpand = true,
            ClipText = true,
            FontColorOverride = participant.Present ? null : Color.Gray,
        };

        var row = Row(swatch, label);
        if (!host || self || participant.Role == CanvasRole.Host)
            return row;

        var block = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 2,
        };
        block.AddChild(row);

        var user = participant.User;
        var buttons = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 4,
            Margin = new Thickness(16, 0, 0, 4),
        };

        switch (participant.Role)
        {
            case CanvasRole.Editor:
                var transfer = WideButton("adt-canvas-session-make-host", () => HostTransferRequested?.Invoke(user));
                transfer.Disabled = !participant.Present;
                buttons.AddChild(transfer);

                var kick = new ConfirmButton
                {
                    Text = Loc.GetString("adt-canvas-session-kick"),
                    ConfirmationText = Loc.GetString("adt-canvas-session-confirm"),
                    HorizontalExpand = true,
                };
                kick.OnPressed += _ => KickRequested?.Invoke(user);
                buttons.AddChild(kick);
                break;

            case CanvasRole.Viewer:
                var invite = WideButton("adt-canvas-session-invite", () => InviteRequested?.Invoke(user));
                invite.Disabled = full;
                buttons.AddChild(invite);
                break;
        }

        block.AddChild(buttons);
        return block;
    }

    private static Button SmallButton(string loc, Action action)
    {
        var button = new Button
        {
            Text = Loc.GetString(loc),
        };
        button.OnPressed += _ => action();
        return button;
    }

    private static Button WideButton(string loc, Action action)
    {
        var button = SmallButton(loc, action);
        button.HorizontalExpand = true;
        return button;
    }

    private static BoxContainer Row(params Control[] children)
    {
        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 4,
        };

        foreach (var child in children)
        {
            row.AddChild(child);
        }

        return row;
    }
}
