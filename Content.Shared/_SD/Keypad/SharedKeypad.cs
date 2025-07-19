using Robust.Shared.Serialization;

namespace Content.Shared._SD.Keypad;

[Serializable, NetSerializable]
public enum KeypadUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class KeypadUISate : BoundUserInterfaceState
{
    public string CurrentCode { get; }
    public KeypadState State { get; }

    public KeypadUISate(string currentCode, KeypadState state)
    {
        CurrentCode = currentCode;
        State = state;
    }
}

[Serializable, NetSerializable]
public sealed class KeypadKeypadPressedMessage : BoundUserInterfaceMessage
{
    public readonly int Button;

    public KeypadKeypadPressedMessage(int button)
    {
        Button = button;
    }
}

[Serializable, NetSerializable]
public sealed class KeypadClearButtonPressedMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class KeypadEnterButtonPressedMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class KeypadCancelButtonPressedMessage : BoundUserInterfaceMessage
{
}
