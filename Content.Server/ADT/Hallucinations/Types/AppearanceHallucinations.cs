using Content.Shared.ADT.Hallucinations.Events;

namespace Content.Server.ADT.Hallucinations.Types;

public sealed partial class AppearanceHallucinations : BaseHallucinationsType
{
    [DataField]
    public List<HallucinationAppearanceData> Appearances = new();
}
