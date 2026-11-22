using Content.Shared.Guidebook;
using Content.Shared.Materials;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Power.Generation.FissionGenerator;

// ADT: адаптировано под ADT — свойства материала хранятся локально,
// без зависимости от глобального MaterialPrototype.Properties.
public abstract partial class GasTurbinePartComponent : Component
{
    [DataField("material")]
    public ProtoId<MaterialPrototype> Material = "Steel";

    public MaterialProperties Properties
    {
        get
        {
            _properties ??= ReactorMaterialDefaults.GetFor(Material);
            return _properties;
        }
        set => _properties = value;
    }
    [DataField("properties")]
    private MaterialProperties? _properties;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class GasTurbineBladeComponent : GasTurbinePartComponent
{
    [GuidebookData]
    public float GuidebookIntegrity => Math.Max(1, 5 * Properties.Hardness);

    [GuidebookData]
    public float GuidebookInertia => Math.Max(200, 200 * Properties.Density);
}

[RegisterComponent, NetworkedComponent]
public sealed partial class GasTurbineStatorComponent : GasTurbinePartComponent
{
    [GuidebookData]
    public float GuidebookEfficiency => Math.Max(0.2f, 0.2f * Properties.ElectricalConductivity);
}
