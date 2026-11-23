using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Virology.Pandemic;

[Serializable, NetSerializable]
public enum ADTPandemicUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum ADTPandemicVisuals : byte
{
    HasBeaker,
    Busy,
}

[Serializable, NetSerializable]
public sealed class ADTPandemicDiseaseInfo
{
    public int Index;

    public string Name = string.Empty;

    public string Agent = string.Empty;

    public string Description = string.Empty;

    public string Route = string.Empty;

    public string Cure = string.Empty;

    public bool CanImmunity;

    public bool IsAdvance;

    public List<(string Id, string Name)> Symptoms = new();
}

[Serializable, NetSerializable]
public sealed class ADTPandemicAntibodyInfo
{
    public int Index;

    public string Name = string.Empty;

    public bool CanVaccine;
}

[Serializable, NetSerializable]
public sealed class ADTPandemicBoundUserInterfaceState : BoundUserInterfaceState
{
    public bool HasBeaker;

    public bool ReadOnly;

    public string? ErrorMessage;

    public string BloodDna = string.Empty;

    public bool Busy;

    public List<ADTPandemicDiseaseInfo> Diseases = new();

    public List<ADTPandemicAntibodyInfo> Antibodies = new();

    public List<(string Id, string Name)> SymptomCandidates = new();

    public int CandidatesForDisease = -1;
}

[Serializable, NetSerializable]
public sealed class ADTPandemicEjectBeakerMessage : BoundUserInterfaceMessage
{
    public bool Clear;

    public ADTPandemicEjectBeakerMessage(bool clear)
    {
        Clear = clear;
    }
}

[Serializable, NetSerializable]
public sealed class ADTPandemicRenameDiseaseMessage : BoundUserInterfaceMessage
{
    public int Index;

    public string Name;

    public ADTPandemicRenameDiseaseMessage(int index, string name)
    {
        Index = index;
        Name = name;
    }
}

[Serializable, NetSerializable]
public sealed class ADTPandemicCreateCultureMessage : BoundUserInterfaceMessage
{
    public int Index;

    public ADTPandemicCreateCultureMessage(int index)
    {
        Index = index;
    }
}

[Serializable, NetSerializable]
public sealed class ADTPandemicCreateVaccineMessage : BoundUserInterfaceMessage
{
    public int Index;

    public ADTPandemicCreateVaccineMessage(int index)
    {
        Index = index;
    }
}

[Serializable, NetSerializable]
public sealed class ADTPandemicPrintFormMessage : BoundUserInterfaceMessage
{
    public int Index;

    public ADTPandemicPrintFormMessage(int index)
    {
        Index = index;
    }
}

[Serializable, NetSerializable]
public sealed class ADTPandemicRequestSymptomsMessage : BoundUserInterfaceMessage
{
    public int Index;

    public ADTPandemicRequestSymptomsMessage(int index)
    {
        Index = index;
    }
}

[Serializable, NetSerializable]
public sealed class ADTPandemicAddSymptomMessage : BoundUserInterfaceMessage
{
    public int Index;

    public string SymptomId;

    public ADTPandemicAddSymptomMessage(int index, string symptomId)
    {
        Index = index;
        SymptomId = symptomId;
    }
}

[Serializable, NetSerializable]
public sealed class ADTPandemicRemoveSymptomMessage : BoundUserInterfaceMessage
{
    public int Index;

    public string SymptomId;

    public ADTPandemicRemoveSymptomMessage(int index, string symptomId)
    {
        Index = index;
        SymptomId = symptomId;
    }
}
