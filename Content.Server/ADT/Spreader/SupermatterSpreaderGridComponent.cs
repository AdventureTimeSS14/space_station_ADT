namespace Content.Server.ADT.Spreader;

[RegisterComponent]
public sealed partial class SupermatterSpreaderGridComponent : Component
{
    [DataField]
    public float UpdateAccumulator = SupermatterSpreaderSystem.SpreadCooldownSeconds;
}
