using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Virology;

[Flags, Serializable, NetSerializable]
public enum ADTDiseaseSpread : byte
{
    None = 0,
    NonContagious = 1 << 0,
    Bites = 1 << 1,
    Blood = 1 << 2,
    Contact = 1 << 3,
    Airborne = 1 << 4,
}

[Flags, Serializable, NetSerializable]
public enum ADTDiseaseVisibility : byte
{
    Visible = 0,
    HiddenHud = 1 << 0,
    HiddenScanner = 1 << 1,
    HiddenPandemic = 1 << 2,
}

[Serializable, NetSerializable]
public enum ADTDiseaseSeverity : byte
{
    Positive,
    Nonthreat,
    Minor,
    Medium,
    Harmful,
    Dangerous,
    Biohazard,
    Uncurable,
}

[Serializable, NetSerializable]
public enum ADTSymptomPhase : byte
{
    Start,
    Activate,
    End,
}
