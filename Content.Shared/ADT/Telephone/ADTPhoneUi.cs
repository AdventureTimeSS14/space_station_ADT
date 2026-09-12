using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Telephone;

/// <summary>
/// UI key for the handheld telephone window.
/// </summary>
[Serializable, NetSerializable]
public enum ADTPhoneUiKey : byte
{
    Key,
}

/// <summary>
/// Telephone window state: list of other phones, do-not-disturb and call status.
/// </summary>
[Serializable, NetSerializable]
public sealed class ADTPhoneBuiState : BoundUserInterfaceState
{
    public readonly List<ADTPhoneInfo> Phones;
    public readonly bool DoNotDisturb;
    public readonly bool Engaged;
    public readonly bool Ringing;

    public ADTPhoneBuiState(List<ADTPhoneInfo> phones, bool doNotDisturb, bool engaged, bool ringing)
    {
        Phones = phones;
        DoNotDisturb = doNotDisturb;
        Engaged = engaged;
        Ringing = ringing;
    }
}

/// <summary>
/// Phone id and display name shown in the telephone list.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct ADTPhoneInfo(NetEntity Id, string Name);

/// <summary>
/// Call the phone with the given id.
/// </summary>
[Serializable, NetSerializable]
public sealed class ADTPhoneCallMsg : BoundUserInterfaceMessage
{
    public readonly NetEntity Id;

    public ADTPhoneCallMsg(NetEntity id)
    {
        Id = id;
    }
}

/// <summary>
/// Toggle the do-not-disturb mode.
/// </summary>
[Serializable, NetSerializable]
public sealed class ADTPhoneDoNotDisturbMsg : BoundUserInterfaceMessage
{
    public readonly bool DoNotDisturb;

    public ADTPhoneDoNotDisturbMsg(bool doNotDisturb)
    {
        DoNotDisturb = doNotDisturb;
    }
}

/// <summary>
/// Answer the incoming call.
/// </summary>
[Serializable, NetSerializable]
public sealed class ADTPhoneAnswerMsg : BoundUserInterfaceMessage;

/// <summary>
/// Hang up the current call.
/// </summary>
[Serializable, NetSerializable]
public sealed class ADTPhoneHangUpMsg : BoundUserInterfaceMessage;
