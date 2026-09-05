using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.NanoChat;

/// <summary>
///     Added to a station AI while it is held in a core (via the AiHeld prototype).
///     Marks its NanoChat card as deliverable without a PDA cartridge and grants the NanoChat action.
/// </summary>
[RegisterComponent]
public sealed partial class StationAiNanoChatComponent : Component
{
    /// <summary>
    ///     The action that opens the NanoChat UI.
    /// </summary>
    [DataField]
    public EntProtoId Action = "ActionStationAiNanoChat";

    [DataField]
    public EntityUid? ActionEntity;

    /// <summary>
    ///     The <see cref="RadioChannelPrototype" /> required to send or receive messages.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> RadioChannel = "Common";
}
