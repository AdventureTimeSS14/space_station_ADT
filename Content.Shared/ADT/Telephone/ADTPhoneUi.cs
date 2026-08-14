using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Telephone;

[Serializable, NetSerializable]
public enum ADTPhoneUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ADTPhoneBuiState : BoundUserInterfaceState
{
    public readonly List<ADTPhoneInfo> Phones;
    public readonly bool Dnd;
    public readonly bool Engaged;
    public readonly bool Ringing;

    public ADTPhoneBuiState(List<ADTPhoneInfo> phones, bool dnd, bool engaged, bool ringing)
    {
        Phones = phones;
        Dnd = dnd;
        Engaged = engaged;
        Ringing = ringing;
    }
}

[Serializable, NetSerializable]
public readonly record struct ADTPhoneInfo(NetEntity Id, string Name);

[Serializable, NetSerializable]
public sealed class ADTPhoneCallMsg : BoundUserInterfaceMessage
{
    public readonly NetEntity Id;

    public ADTPhoneCallMsg(NetEntity id)
    {
        Id = id;
    }
}

[Serializable, NetSerializable]
public sealed class ADTPhoneDndMsg : BoundUserInterfaceMessage
{
    public readonly bool Dnd;

    public ADTPhoneDndMsg(bool dnd)
    {
        Dnd = dnd;
    }
}

[Serializable, NetSerializable]
public sealed class ADTPhoneAnswerMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class ADTPhoneHangUpMsg : BoundUserInterfaceMessage;
