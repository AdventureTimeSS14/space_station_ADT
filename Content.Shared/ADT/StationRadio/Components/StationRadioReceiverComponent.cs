using Robust.Shared.GameStates;

namespace Content.Shared.ADT.StationRadio.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StationRadioReceiverComponent : Component
{
    /// <summary>
    /// The sound entity being played
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? SoundEntity;

    /// <summary>
    /// Is the radio turned on
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active = true;

    /// <summary>
    /// Currently selected radio channel ID (e.g. "ADTOldBroadcast")
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? SelectedChannelId;
}
