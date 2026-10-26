namespace Content.Server.ADT.Xenobiology;

[RegisterComponent]
public sealed partial class SlimeAttackOrderComponent : Component
{
    public List<EntityUid> Slimes = [];

    public TimeSpan ExpiresAt;
}
