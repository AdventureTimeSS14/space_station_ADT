using System.Collections.Generic;
using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.ADT.Radio.Components;

/// <summary>
/// Universal component for selecting radio channels via verbs.
/// Added to entities that have RadioMicrophoneComponent and/or RadioSpeakerComponent.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VerbSelectableRadioChannelComponent : Component
{
    /// <summary>
    /// List of allowed radio channels that can be selected. If empty, all channels are allowed.
    /// </summary>
    [DataField("allowedChannels", customTypeSerializer: typeof(PrototypeIdListSerializer<RadioChannelPrototype>))]
    public List<string> AllowedChannelIds = new();

    /// <summary>
    /// Currently selected channel.
    /// </summary>
    [DataField]
    public string SelectedChannelId = "Common";
}

[Serializable, NetSerializable]
public sealed class VerbSelectableRadioChannelComponentState : ComponentState
{
    public string SelectedChannelId { get; }

    public VerbSelectableRadioChannelComponentState(string selectedChannelId)
    {
        SelectedChannelId = selectedChannelId;
    }
}
