namespace Content.Shared.ADT.Fishing.Components;

[RegisterComponent]
public sealed partial class FishComponent : Component
{
    public const float DefaultDifficulty = 0.02f;

    [DataField]
    public float FishDifficulty = DefaultDifficulty;
}
