namespace Content.Server.ADT.MedicalAssembler;

[RegisterComponent]
[Access(typeof(ADTMedicalAssemblerSystem))]
public sealed partial class ADTMedicalAssemblerComponent : Component
{
    public const string MaterialsContainerId = "microwave_entity_container";
    public const string BeakerSlotId = "beakerSlot";

    [DataField]
    public float MaxCustomVolume = 5f;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool CustomTabUnlocked;

    [DataField]
    public Dictionary<string, string> MedipenStylePrototypes = new();
}
