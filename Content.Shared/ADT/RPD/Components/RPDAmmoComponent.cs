using Content.Shared.ADT.RPD.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.RPD.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RPDAmmoSystem))]
public sealed partial class RPDAmmoComponent : Component
{
    /// <summary>
    /// How many charges are contained in this ammo cartridge.
    /// Can be partially transferred into an RPD, until it is empty then it gets deleted.
    /// </summary>
    [DataField("charges"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public int Charges = 50;
}
