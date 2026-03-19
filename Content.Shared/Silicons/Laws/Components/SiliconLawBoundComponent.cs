using Content.Shared.Actions;
using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;

namespace Content.Shared.Silicons.Laws.Components;

/// <summary>
/// This is used for entities which are bound to silicon laws and can view them.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSiliconLawSystem))]
public sealed partial class SiliconLawBoundComponent : Component
{
    /// <summary>
    /// The last entity that provided laws to this entity.
    /// </summary>
    [DataField]
    public EntityUid? LastLawProvider;
    // START-ADT TWEAK FIx
    /// <summary>
    /// The sound that plays for the Silicon player
    /// when the law change is processed for the provider.
    /// </summary>
    [DataField]
    public SoundSpecifier? LawUploadSound = new SoundPathSpecifier("/Audio/Misc/cryo_warning.ogg");
    // ADT-END
}

/// <summary>
/// Event raised to get the laws that a law-bound entity has.
///
/// Is first raised on the entity itself, then on the
/// entity's station, then on the entity's grid,
/// before being broadcast.
/// </summary>
/// <param name="Entity"></param>
[ByRefEvent]
public record struct GetSiliconLawsEvent(EntityUid Entity)
{
    public EntityUid Entity = Entity;

    public SiliconLawset Laws = new();

    public bool Handled = false;
}

public sealed partial class ToggleLawsScreenEvent : InstantActionEvent
{

}

[NetSerializable, Serializable]
public enum SiliconLawsUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SiliconLawBuiState : BoundUserInterfaceState
{
    public List<SiliconLawData> Laws; // ADT-Tweak
    public HashSet<ProtoId<RadioChannelPrototype>>? RadioChannels;

    public SiliconLawBuiState(List<SiliconLawData> laws, HashSet<ProtoId<RadioChannelPrototype>>? radioChannels) // ADT-Tweak
    {
        Laws = laws;
        RadioChannels = radioChannels;
    }
}

// ADT-Tweak start
/// <summary>
/// Serializable data for a silicon law, used for UI state.
/// </summary>
[NetSerializable, Serializable]
public sealed class SiliconLawData
{
    public string LawString = string.Empty;
    public string Order = string.Empty;
    public string? LawIdentifierOverride;

    public SiliconLawData()
    {
    }

    public SiliconLawData(string lawString, string order, string? lawIdentifierOverride)
    {
        LawString = lawString;
        Order = order;
        LawIdentifierOverride = lawIdentifierOverride;
    }

    public static SiliconLawData FromSiliconLaw(SiliconLaw law)
    {
        return new SiliconLawData(law.LawString, law.Order.ToString(), law.LawIdentifierOverride);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not SiliconLawData other)
            return false;

        return LawString == other.LawString
               && Order == other.Order
               && LawIdentifierOverride == other.LawIdentifierOverride;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(LawString, Order, LawIdentifierOverride);
    }
}
// ADT-Tweak end

