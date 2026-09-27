using Content.Shared.ADT.RPD.Systems;
using Content.Shared.Materials;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.RPD.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RPDMaterialChargesSystem))]
public sealed partial class RPDMaterialChargesComponent : Component
{
    [DataField]
    public SoundSpecifier InsertSound = new SoundCollectionSpecifier("MachineInsert");

    [DataField]
    public Dictionary<ProtoId<MaterialPrototype>, RPDMaterialChargeRate> ChargeRates = new();

    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<MaterialPrototype>, int> Remainder = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class RPDMaterialChargeRate
{
    [DataField]
    public int Charges = 1;

    [DataField]
    public int Sheets = 1;
}
