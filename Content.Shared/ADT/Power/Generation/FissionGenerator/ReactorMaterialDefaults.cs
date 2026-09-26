namespace Content.Shared.ADT.Power.Generation.FissionGenerator;

/// <summary>
/// ADT: балансные значения свойств материалов для реактора/турбины.
/// Заменяет зависимость от глобального MaterialPrototype.Properties —
/// значения скопированы из FarHorizons-прототипов материалов.
/// </summary>
public static class ReactorMaterialDefaults
{
    private static readonly Dictionary<string, MaterialProperties> Defaults = new()
    {
        ["Steel"] = new MaterialProperties
        {
            ElectricalConductivity = 5, ThermalConductivity = 6, Hardness = 3, Density = 4, ChemicalResistance = 6,
        },
        ["Gold"] = new MaterialProperties
        {
            ElectricalConductivity = 7, ThermalConductivity = 7, Hardness = 2, Density = 6, ChemicalResistance = 6,
        },
        ["Silver"] = new MaterialProperties
        {
            ElectricalConductivity = 6, ThermalConductivity = 6, Hardness = 2, Density = 4, ChemicalResistance = 6,
        },
        ["Brass"] = new MaterialProperties
        {
            ElectricalConductivity = 6, ThermalConductivity = 7, Hardness = 2, Density = 5, ChemicalResistance = 7,
        },
        ["Plasteel"] = new MaterialProperties
        {
            ElectricalConductivity = 5, ThermalConductivity = 6, Hardness = 3, Density = 7, ChemicalResistance = 6,
        },
        ["Glass"] = new MaterialProperties
        {
            ElectricalConductivity = 3, ThermalConductivity = 3, Hardness = 3, Density = 2, ChemicalResistance = 5,
        },
        ["PlasmaGlass"] = new MaterialProperties
        {
            ElectricalConductivity = 3, ThermalConductivity = 3, Hardness = 7, Density = 3, ChemicalResistance = 5,
        },
        ["Diamond"] = new MaterialProperties
        {
            ElectricalConductivity = 3, ThermalConductivity = 3, Hardness = 7, Density = 6, ChemicalResistance = 5,
        },
        ["Cerenkite"] = new MaterialProperties
        {
            ElectricalConductivity = 6, ThermalConductivity = 6, Hardness = 2, Density = 4, ChemicalResistance = 6,
            Radioactivity = 5,
        },
        ["Uranium"] = new MaterialProperties
        {
            ElectricalConductivity = 4, ThermalConductivity = 3, Hardness = 5, Density = 7, ChemicalResistance = 6,
            Radioactivity = 4, NeutronRadioactivity = 3,
        },
        ["Plutonium"] = new MaterialProperties
        {
            ElectricalConductivity = 7, ThermalConductivity = 6, Hardness = 7, Density = 8, ChemicalResistance = 6,
            Radioactivity = 3, NeutronRadioactivity = 5,
        },
        ["Bananium"] = new MaterialProperties
        {
            ElectricalConductivity = 4, ThermalConductivity = 4, Hardness = 4, Density = 5, ChemicalResistance = 4.5f,
            Radioactivity = 7, NeutronRadioactivity = 1.5f,
        },
        ["Plasma"] = new MaterialProperties
        {
            ElectricalConductivity = 5, ThermalConductivity = 3, Hardness = 2, Density = 1, ChemicalResistance = 5,
            Radioactivity = 2, ActivePlasma = 10,
        },
        ["UraniumGlass"] = new MaterialProperties
        {
            ElectricalConductivity = 3.5f, ThermalConductivity = 3, Hardness = 4, Density = 4.5f, ChemicalResistance = 5.5f,
            Radioactivity = 2, NeutronRadioactivity = 1.5f,
        },
        ["Bohrum"] = new MaterialProperties
        {
            ElectricalConductivity = 5, ThermalConductivity = 6, Hardness = 5, Density = 6, ChemicalResistance = 7,
        },
        ["Meaterial"] = new MaterialProperties
        {
            ElectricalConductivity = 4, Hardness = 1, Density = 3, Flammability = 3, Radioactivity = 5,
        },
    };

    public static MaterialProperties GetFor(string material)
    {
        if (Defaults.TryGetValue(material, out var props))
            return new MaterialProperties(props);
        return new MaterialProperties();
    }
}
