using Content.Shared.Actions;
using Content.Shared.ADT.Language;
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
    [DataField("languages")]
    public Dictionary<string, LanguageKnowledge> Languages = new();

    /// <summary>
    /// The entity this implant is inside
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? ImplantedEntity;

    /// <summary>
    /// Should this implant be removeable?
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("permanent"), AutoNetworkedField]
    public bool Permanent = false;
}
