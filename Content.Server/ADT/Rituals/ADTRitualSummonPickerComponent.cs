namespace Content.Server.ADT.Rituals;

[RegisterComponent]
public sealed partial class ADTRitualSummonPickerComponent : Component
{
    [ViewVariables]
    public EntityUid Shaman;

    [ViewVariables]
    public readonly HashSet<EntityUid> Candidates = new();
}
