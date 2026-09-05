using Robust.Shared.Serialization;

namespace Content.Shared.ADT.CartridgeLoader.Cartridges;

/// <summary>
///     A group chat in NanoChat. Group data is replicated on every member card;
///     the server is the source of truth and rewrites it on every group operation.
/// </summary>
[Serializable, NetSerializable, DataRecord]
public partial struct NanoChatGroup
{
    /// <summary>
    ///     Unique group number.
    /// </summary>
    public uint Number;

    /// <summary>
    ///     Display name of the group.
    /// </summary>
    public string Name;

    /// <summary>
    ///     Whether anyone can join the group without an invitation.
    /// </summary>
    public bool IsPublic;

    /// <summary>
    ///     NanoChat number of the group owner.
    /// </summary>
    public uint Owner;

    /// <summary>
    ///     All members of the group, including the owner.
    /// </summary>
    public List<NanoChatMember> Members;

    /// <summary>
    ///     Whether the group has unread messages.
    /// </summary>
    public bool HasUnread;

    public NanoChatGroup(
        uint number,
        string name,
        bool isPublic,
        uint owner,
        List<NanoChatMember> members,
        bool hasUnread = false)
    {
        Number = number;
        Name = name;
        IsPublic = isPublic;
        Owner = owner;
        Members = members;
        HasUnread = hasUnread;
    }
}

/// <summary>
///     A single member of a group chat.
/// </summary>
[Serializable, NetSerializable, DataRecord]
public partial struct NanoChatMember
{
    /// <summary>
    ///     The member's NanoChat number.
    /// </summary>
    public uint Number;

    /// <summary>
    ///     The member's display name.
    /// </summary>
    public string Name;

    /// <summary>
    ///     The member's job title, if available.
    /// </summary>
    public string? JobTitle;

    public NanoChatMember(uint number, string name, string? jobTitle = null)
    {
        Number = number;
        Name = name;
        JobTitle = jobTitle;
    }
}

/// <summary>
///     A pending group invitation stored on the invitee's card.
/// </summary>
[Serializable, NetSerializable, DataRecord]
public partial struct NanoChatGroupInvite
{
    /// <summary>
    ///     Number of the group the invitee was invited to.
    /// </summary>
    public uint GroupNumber;

    /// <summary>
    ///     Name of the group.
    /// </summary>
    public string GroupName;

    /// <summary>
    ///     NanoChat number of the player who sent the invite.
    /// </summary>
    public uint FromNumber;

    /// <summary>
    ///     Name of the player who sent the invite.
    /// </summary>
    public string FromName;

    public NanoChatGroupInvite(uint groupNumber, string groupName, uint fromNumber, string fromName)
    {
        GroupNumber = groupNumber;
        GroupName = groupName;
        FromNumber = fromNumber;
        FromName = fromName;
    }
}

/// <summary>
///     Public info about a group, shown in the group browser.
/// </summary>
[Serializable, NetSerializable, DataRecord]
public partial struct NanoChatGroupInfo
{
    /// <summary>
    ///     Number of the group.
    /// </summary>
    public uint Number;

    /// <summary>
    ///     Name of the group.
    /// </summary>
    public string Name;

    /// <summary>
    ///     Name of the group owner.
    /// </summary>
    public string? OwnerName;

    /// <summary>
    ///     Current member count.
    /// </summary>
    public int MemberCount;

    public NanoChatGroupInfo(uint number, string name, string? ownerName, int memberCount)
    {
        Number = number;
        Name = name;
        OwnerName = ownerName;
        MemberCount = memberCount;
    }
}
