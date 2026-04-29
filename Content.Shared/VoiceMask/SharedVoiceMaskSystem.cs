using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.VoiceMask;

[Serializable, NetSerializable]
public enum VoiceMaskUIKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class VoiceMaskBuiState : BoundUserInterfaceState
{
    public readonly string Name;
    public readonly string? Verb;
    public readonly string Voice; // Corvax-TTS
    public readonly string Bark; // ADT Barks
    public readonly float Pitch; // ADT Barks
    public readonly string? JobIconId; // ADT-Tweak start
    public readonly bool Active;
    public readonly bool AccentHide;

    public VoiceMaskBuiState(string name, string voice, string bark, float pitch, string? verb, bool active, bool accentHide, string? jobIconId = null)
    {
        Name = name;
        Verb = verb;
        Voice = voice;
        Bark = bark;
        Pitch = pitch;
        JobIconId = jobIconId;
        Active = active;
        AccentHide = accentHide;
    }
}

[Serializable, NetSerializable]
public sealed class VoiceMaskChangeNameMessage : BoundUserInterfaceMessage
{
    public readonly string Name;

    public VoiceMaskChangeNameMessage(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Change the speech verb prototype to override, or null to use the user's verb.
/// </summary>
[Serializable, NetSerializable]
public sealed class VoiceMaskChangeVerbMessage : BoundUserInterfaceMessage
{
    public readonly string? Verb;

    public VoiceMaskChangeVerbMessage(string? verb)
    {
        Verb = verb;
    }
}

/// <summary>
/// ADT-Tweak
/// Change the job icon that will be displayed in radio chat.
/// </summary>
[Serializable, NetSerializable]
public sealed class VoiceMaskChangeJobIconMessage : BoundUserInterfaceMessage
{
    public readonly ProtoId<JobIconPrototype>? JobIconId;

    public VoiceMaskChangeJobIconMessage(ProtoId<JobIconPrototype>? jobIconId)
    {
        JobIconId = jobIconId;
    }
}

/// <summary>
///     Toggle the effects of the voice mask.
/// </summary>
[Serializable, NetSerializable]
public sealed class VoiceMaskToggleMessage : BoundUserInterfaceMessage;

/// <summary>
///     Toggle the effects of accent negation.
/// </summary>
[Serializable, NetSerializable]
public sealed class VoiceMaskAccentToggleMessage : BoundUserInterfaceMessage;
