using Content.Shared.Actions;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Implants.Components;

/// <summary>
/// This component grants language knowledge when implanted.
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
