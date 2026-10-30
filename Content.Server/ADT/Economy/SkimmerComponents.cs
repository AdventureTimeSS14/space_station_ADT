namespace Content.Server.ADT.Economy;

[RegisterComponent]
public sealed partial class SkimmerComponent : Component
{
    [DataField]
    public float AttachDelay = 6f;
}

[RegisterComponent]
public sealed partial class ATMSkimmerComponent : Component
{
    [DataField]
    public int ScansLeft = 2;

    [DataField]
    public int? LinkedAccountId;

    [DataField]
    public float StealFraction = 0.25f;

    [DataField]
    public int StealCap = 2000;
}
