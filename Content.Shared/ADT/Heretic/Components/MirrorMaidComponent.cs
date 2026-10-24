//

using Robust.Shared.GameStates;

namespace Content.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class MirrorMaidComponent : Component
{
    [DataField]
    public float ExamineDamage = 10f;

    [DataField]
    public TimeSpan ExamineDelay = TimeSpan.FromSeconds(30);
}
