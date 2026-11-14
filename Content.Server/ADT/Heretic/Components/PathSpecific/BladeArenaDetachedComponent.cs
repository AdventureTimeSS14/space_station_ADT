//

using Robust.Shared.Map;

namespace Content.Server.Heretic.Components.PathSpecific;

[RegisterComponent]
public sealed partial class BladeArenaDetachedComponent : Component
{
    [DataField]
    public EntityCoordinates OriginalCoords;

    [DataField]
    public Angle OriginalRotation;
}
