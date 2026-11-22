using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Drawing;

[Serializable, NetSerializable]
public enum CanvasRole : byte
{
    Viewer,
    Editor,
    Host,
}

[Serializable, NetSerializable]
public sealed class CanvasParticipantInfo
{
    public readonly NetUserId User;
    public readonly byte Index;
    public readonly string Name;
    public readonly CanvasRole Role;
    public readonly bool Present;

    public CanvasParticipantInfo(NetUserId user, byte index, string name, CanvasRole role, bool present)
    {
        User = user;
        Index = index;
        Name = name;
        Role = role;
        Present = present;
    }
}

[Serializable, NetSerializable]
public sealed class CanvasJoinInfo
{
    public readonly NetUserId User;
    public readonly string Name;
    public readonly bool Invite;

    public CanvasJoinInfo(NetUserId user, string name, bool invite)
    {
        User = user;
        Name = name;
        Invite = invite;
    }
}

[Serializable, NetSerializable]
public sealed class CanvasEaselBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly bool HasCanvas;
    public readonly Vector2i CanvasSize;
    public readonly bool CanvasPainted;
    public readonly NetUserId? CanvasOwner;
    public readonly bool SessionActive;
    public readonly NetUserId? Host;
    public readonly string? HostName;
    public readonly Guid? Painting;
    public readonly int MaxEditors;
    public readonly List<CanvasParticipantInfo> Participants;
    public readonly List<CanvasJoinInfo> Requests;

    public CanvasEaselBoundUserInterfaceState(bool hasCanvas, Vector2i canvasSize, bool canvasPainted,
        NetUserId? canvasOwner, bool sessionActive, NetUserId? host, string? hostName, Guid? painting,
        int maxEditors, List<CanvasParticipantInfo> participants, List<CanvasJoinInfo> requests)
    {
        HasCanvas = hasCanvas;
        CanvasSize = canvasSize;
        CanvasPainted = canvasPainted;
        CanvasOwner = canvasOwner;
        SessionActive = sessionActive;
        Host = host;
        HostName = hostName;
        Painting = painting;
        MaxEditors = maxEditors;
        Participants = participants;
        Requests = requests;
    }
}

[Serializable, NetSerializable]
public sealed class CanvasDraftInfo
{
    public readonly Guid PaintingUid;
    public readonly string Title;
    public readonly int Width;
    public readonly int Height;
    public readonly DateTime UpdatedAt;

    public CanvasDraftInfo(Guid paintingUid, string title, int width, int height, DateTime updatedAt)
    {
        PaintingUid = paintingUid;
        Title = title;
        Width = width;
        Height = height;
        UpdatedAt = updatedAt;
    }
}

[Serializable, NetSerializable]
public sealed class CanvasEaselDraftsMessage : BoundUserInterfaceMessage
{
    public readonly List<CanvasDraftInfo> Drafts;
    public readonly List<CanvasDraftInfo> Pending;
    public readonly List<CanvasDraftInfo> Approved;
    public readonly int Slots;

    public CanvasEaselDraftsMessage(List<CanvasDraftInfo> drafts, List<CanvasDraftInfo> pending,
        List<CanvasDraftInfo> approved, int slots)
    {
        Drafts = drafts;
        Pending = pending;
        Approved = approved;
        Slots = slots;
    }
}

[Serializable, NetSerializable]
public sealed class CanvasEaselStartMessage : BoundUserInterfaceMessage
{
    public readonly Guid? Draft;
    public readonly Guid? Parent;

    public CanvasEaselStartMessage(Guid? draft, Guid? parent = null)
    {
        Draft = draft;
        Parent = parent;
    }
}

[Serializable, NetSerializable]
public sealed class CanvasEaselCloseSessionMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class CanvasEaselSubmitMessage : BoundUserInterfaceMessage
{
    public readonly string Title;
    public readonly string Description;

    public CanvasEaselSubmitMessage(string title, string description)
    {
        Title = title;
        Description = description;
    }
}

[Serializable, NetSerializable]
public sealed class CanvasEaselWithdrawMessage : BoundUserInterfaceMessage
{
    public readonly Guid Painting;

    public CanvasEaselWithdrawMessage(Guid painting)
    {
        Painting = painting;
    }
}

[Serializable, NetSerializable]
public sealed class CanvasEaselJoinRequestMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class CanvasEaselJoinCancelMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class CanvasEaselJoinDecisionMessage : BoundUserInterfaceMessage
{
    public readonly NetUserId User;
    public readonly bool Accept;

    public CanvasEaselJoinDecisionMessage(NetUserId user, bool accept)
    {
        User = user;
        Accept = accept;
    }
}

[Serializable, NetSerializable]
public sealed class CanvasEaselInviteMessage : BoundUserInterfaceMessage
{
    public readonly NetUserId User;

    public CanvasEaselInviteMessage(NetUserId user)
    {
        User = user;
    }
}

[Serializable, NetSerializable]
public sealed class CanvasEaselInviteResponseMessage : BoundUserInterfaceMessage
{
    public readonly bool Accept;

    public CanvasEaselInviteResponseMessage(bool accept)
    {
        Accept = accept;
    }
}

[Serializable, NetSerializable]
public sealed class CanvasEaselKickMessage : BoundUserInterfaceMessage
{
    public readonly NetUserId User;

    public CanvasEaselKickMessage(NetUserId user)
    {
        User = user;
    }
}

[Serializable, NetSerializable]
public sealed class CanvasEaselTransferHostMessage : BoundUserInterfaceMessage
{
    public readonly NetUserId User;

    public CanvasEaselTransferHostMessage(NetUserId user)
    {
        User = user;
    }
}

[Serializable, NetSerializable]
public sealed class CanvasEaselLeaveMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class CanvasEaselRevertMessage : BoundUserInterfaceMessage
{
}
