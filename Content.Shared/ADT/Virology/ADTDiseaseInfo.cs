using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Virology;

[Serializable, NetSerializable]
public sealed class ADTDiseaseInfo
{
    public string Name = string.Empty;

    public string Form = string.Empty;

    public string Cure = string.Empty;

    public string Route = string.Empty;

    public int Stage;

    public int MaxStages;

    public ADTDiseaseSeverity Severity;
}
