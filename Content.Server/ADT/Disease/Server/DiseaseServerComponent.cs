using Content.Shared.ADT.Disease;

namespace Content.Server.ADT.Disease.Server;

[RegisterComponent]
public sealed partial class DiseaseServerComponent : Component
{
    /// <summary>
    /// Which diseases this server has information on.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public List<DiseasePrototype> Diseases = new();
}
