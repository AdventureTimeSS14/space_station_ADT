using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.Radio.Components;

/// <summary>
/// Универсальный компонент для выбора радио-канала через verbs.
/// Добавляется к сущностям, у которых есть RadioMicrophoneComponent и/или RadioSpeakerComponent.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class VerbSelectableRadioChannelComponent : Component
{
    /// <summary>
    /// Список разрешённых каналов. Если пустой — разрешены все RadioChannelPrototype.
    /// </summary>
    [DataField("allowedChannels", customTypeSerializer: typeof(PrototypeIdListSerializer<RadioChannelPrototype>))]
    public List<string> AllowedChannelIds = new();

    /// <summary>
    /// Текущий выбранный канал.
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