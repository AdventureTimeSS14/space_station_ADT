using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RustInRadiusOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField, AutoNetworkedField]
    public float Range = 3f;

    [DataField, AutoNetworkedField]
    public float LookupRange = 0.5f;

    [DataField, AutoNetworkedField]
    public int RustStrength = 10;

    [DataField, AutoNetworkedField]
    public string TileRune = "TileHereticRustRune";
}
