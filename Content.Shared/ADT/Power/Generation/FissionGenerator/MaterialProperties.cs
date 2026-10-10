namespace Content.Shared.ADT.Power.Generation.FissionGenerator;

/// <summary>
/// Физические свойства материала, важные для симуляции реактора и турбины.
/// Портировано из FarHorizons, адаптировано под ADT: используется как локальный DataField
/// в компонентах деталей, без зависимости от глобального MaterialPrototype.
/// </summary>
[DataDefinition]
public sealed partial class MaterialProperties()
{
    [DataField("electrical")]
    public float ElectricalConductivity = 5;

    [DataField("thermal")]
    public float ThermalConductivity = 5;

    [DataField("hard")]
    public float Hardness = 3;

    [DataField("density")]
    public float Density = 3;

    [DataField("reflective")]
    public float Reflectivity = 0;

    [DataField("flammable")]
    public float Flammability = 1;

    [DataField("chemical")]
    public float ChemicalResistance = 3;

    [DataField("radioactive")]
    public float Radioactivity = 0;

    [DataField("n_radioactive")]
    public float NeutronRadioactivity = 0;

    [DataField("spent_fuel")]
    public float FissileIsotopes = 0;

    [DataField("plasma_offgas")]
    public float ActivePlasma = 0;

    public MaterialProperties(MaterialProperties source) : this()
    {
        ElectricalConductivity = source.ElectricalConductivity;
        ThermalConductivity = source.ThermalConductivity;
        Hardness = source.Hardness;
        Density = source.Density;
        Reflectivity = source.Reflectivity;
        Flammability = source.Flammability;
        ChemicalResistance = source.ChemicalResistance;
        Radioactivity = source.Radioactivity;
        NeutronRadioactivity = source.NeutronRadioactivity;
        FissileIsotopes = source.FissileIsotopes;
        ActivePlasma = source.ActivePlasma;
    }
}
