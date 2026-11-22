using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Mech.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MechMagazineComponent : Component
{
    [DataField("magazinetype", required: true)]
    public string MagazineType = string.Empty;

    [DataField]
    public int Capacity = 30;

    [DataField, AutoNetworkedField]
    public int Rounds = -1;
}
