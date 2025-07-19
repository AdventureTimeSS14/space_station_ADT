using Robust.Shared.Serialization;

namespace Content.Shared._SD.Keypad;

[Serializable, NetSerializable]
public enum KeypadState
{
    Normal,
    AwaitingOldCode,
    AwaitingNewCode
}
