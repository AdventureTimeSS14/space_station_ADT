using Content.Server.ADT.Atmos.EntitySystems;
using Robust.Shared.Audio;

namespace Content.Server.ADT.Atmos.Components;
public enum GasCondenserMode : byte
{
    Condense,

    Evaporate
}

[RegisterComponent]
[Access(typeof(GasEvaporatorSystem))]
public sealed partial class GasEvaporatorComponent : Component
{
    public const string BeakerSlotId = "beakerSlot";

    [DataField]
    public string Inlet = "pipe";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float UnitsPerSecond = 5f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MolesToGasMultiplier = 1.6793f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public GasCondenserMode Mode = GasCondenserMode.Condense;

    [DataField]
    public SoundSpecifier SwitchSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
}
