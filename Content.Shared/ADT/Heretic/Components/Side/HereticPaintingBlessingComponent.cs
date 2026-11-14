using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Heretic.Components.Side;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HereticPaintingBlessingComponent : Component
{
    [DataField, AutoNetworkedField]
    public float TimeLeft = 120f;

    public readonly Dictionary<EntityUid, TimeSpan> Processed = new();
}
