namespace Content.Server.ADT.Medical.CryoPod;

[RegisterComponent]
[Access(typeof(CryoPodMetabolismSystem))]
public sealed partial class CryoPodMetabolismComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public float Multiplier = 1f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Tier = 1f;
}
