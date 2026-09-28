<<<<<<< HEAD
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
=======
using Content.Shared.Inventory;
>>>>>>> wizards-filtered
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
    public readonly string Voice; // ADT-Tweak
    public readonly string Bark; // ADT Barks
    public readonly float Pitch; // ADT Barks
    public readonly string? JobIconId; // ADT-Tweak start
    public readonly bool Active;
    public readonly bool AccentHide;
    public readonly LocId TitleText;

<<<<<<< HEAD
    public VoiceMaskBuiState(string name, string voice, string bark, float pitch, string? verb, bool active, bool accentHide, string? jobIconId = null)
=======
    public VoiceMaskBuiState(string name, string? verb, bool active, bool accentHide, LocId titleText)
>>>>>>> wizards-filtered
    {
        Name = name;
        Verb = verb;
        Voice = voice;
        Bark = bark;
        Pitch = pitch;
        JobIconId = jobIconId;
        Active = active;
        AccentHide = accentHide;
        TitleText = titleText;
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

/// <summary>
///  Fired when a voice mask is turned on.
/// </summary>
/// <param name=="Mask">The voice mask that was turned on</param> 
/// <param name=="Source">The entity that owns the voice mask</param> 
/// <param name=="Active">The new value of the voice mask</param> 
public sealed class VoiceMaskToggledEvent(EntityUid mask, EntityUid source, bool active) : IInventoryRelayEvent
{
    public EntityUid Mask = mask;
    public EntityUid Source = source;
    
    public bool Active = active;

    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.WITHOUT_POCKET;
}
