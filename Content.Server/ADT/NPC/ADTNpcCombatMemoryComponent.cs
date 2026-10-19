namespace Content.Server.ADT.NPC;

[RegisterComponent]
public sealed partial class ADTNpcCombatMemoryComponent : Component
{
    [ViewVariables]
    public bool Hurt;

    [ViewVariables]
    public TimeSpan LastHurt;

    [ViewVariables]
    public bool Blocked;

    [ViewVariables]
    public TimeSpan LastBlocked;

    [DataField]
    public float AllyClearance = 0.7f;

    [ViewVariables]
    public bool Spoke;

    [ViewVariables]
    public TimeSpan LastSpoke;
}
