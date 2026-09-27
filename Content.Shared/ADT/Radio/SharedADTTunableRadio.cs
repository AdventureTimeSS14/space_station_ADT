using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Radio;

[Serializable, NetSerializable]
public enum ADTTunableRadioUiKey : byte
{
    Key,
}

public static class ADTRadioFrequency
{
    public const int Scale = 10;

    public static string Format(int frequency)
    {
        return $"{frequency / Scale}.{Math.Abs(frequency % Scale)}";
    }
}

[Serializable, NetSerializable]
public sealed class ADTTunableRadioSetFrequencyMessage : BoundUserInterfaceMessage
{
    public readonly int Frequency;

    public ADTTunableRadioSetFrequencyMessage(int frequency)
    {
        Frequency = frequency;
    }
}

[Serializable, NetSerializable]
public sealed class ADTTunableRadioToggleMicrophoneMessage : BoundUserInterfaceMessage
{
    public readonly bool Enabled;

    public ADTTunableRadioToggleMicrophoneMessage(bool enabled)
    {
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class ADTTunableRadioToggleSpeakerMessage : BoundUserInterfaceMessage
{
    public readonly bool Enabled;

    public ADTTunableRadioToggleSpeakerMessage(bool enabled)
    {
        Enabled = enabled;
    }
}
