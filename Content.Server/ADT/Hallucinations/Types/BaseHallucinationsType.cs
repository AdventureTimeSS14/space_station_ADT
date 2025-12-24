using Content.Shared.Destructible.Thresholds;

namespace Content.Server.ADT.Shizophrenia;

[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseHallucinationsType
{
    [DataField]
    public MinMax Delay = new();

    public abstract BaseHallucinationsEntry GetEntry();
}
