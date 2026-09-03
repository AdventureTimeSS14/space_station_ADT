using Content.Server.ADT.Hallucinations.Entries;
using Content.Shared.Destructible.Thresholds;

namespace Content.Server.ADT.Hallucinations.Types;

[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseHallucinationsType
{
    [DataField]
    public MinMax Delay = new();

    public abstract BaseHallucinationsEntry GetEntry();
}
