using Content.Shared.Actions;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Implants.Components;

/// <summary>
/// Subdermal implants get stored in a container on an entity and grant the entity special actions
/// The actions can be activated via an action, a passive ability (ie tracking), or a reactive ability (ie on death) or some sort of combination
/// They're added and removed with implanters
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TranslatorImplantComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("toUnderstand")]
    public List<string> ToUnderstand = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("toSpeak")]
    public List<string> ToSpeak = new();

    /// <summary>
    /// The entity this implant is inside
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? ImplantedEntity;

    [ViewVariables(VVAccess.ReadWrite)]
    public List<string> ImplantedToUnderstand = new();

    [ViewVariables(VVAccess.ReadWrite)]
    public List<string> ImplantedToSpeak = new();

    /// <summary>
    /// Should this implant be removeable?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("permanent"), AutoNetworkedField]
    public bool Permanent = false;
}
