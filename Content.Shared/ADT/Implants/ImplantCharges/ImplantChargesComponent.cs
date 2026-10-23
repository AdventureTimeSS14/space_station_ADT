using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Implants.ImplantCharges;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ImplantChargesComponent : Component
{
    [DataField, AutoNetworkedField]
    public int MaxCharges = 3;

    [DataField, AutoNetworkedField]
    public int? Charges;

    [DataField]
    public bool RemoveWhenEmpty = true;

    [DataField]
    public HashSet<string> Keys = new() { "trigger" };

    [DataField]
    public LocId? ActionDescription;

    [DataField]
    public LocId? UsedPopup;

    [DataField]
    public LocId? EmptyPopup;
}
