namespace Content.Server.ADT.Crushers;

[RegisterComponent]
public sealed partial class ADTCrusherSpearReturningComponent : Component
{
    [ViewVariables]
    public List<string> SoftenedFixtures = new();
}
