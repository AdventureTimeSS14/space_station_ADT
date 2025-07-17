using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Maths;

namespace Content.Shared.ADT.Atmos.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SuperconductiveComponent : Component
{
    [DataField("resistancePerTile")]
    public Dictionary<Vector2i, float>? ResistancePerTile = null;

    [DataField("resistance")]
    public float? Resistance = null;

    public bool IsTileBased => ResistancePerTile != null && ResistancePerTile.Count > 0;

    public bool IsGlobal => Resistance != null;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class TemperatureSourceComponent : Component
{
}

[RegisterComponent, NetworkedComponent]
public sealed partial class TemperatureReceiverComponent : Component
{
}
