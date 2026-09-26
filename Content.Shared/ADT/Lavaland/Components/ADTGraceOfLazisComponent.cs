using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Lavaland;

[RegisterComponent, NetworkedComponent]
public sealed partial class ADTGraceOfLazisComponent : Component
{
    [DataField]
    public int Portions = 40;

    [DataField]
    public EntProtoId Portion = "ADTFoodGraceOfLazis";

    [DataField]
    public EntProtoId Leftover = "SpearBone";

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(3);

    [DataField]
    public EntityWhitelist? Whitelist;
}

[Serializable, NetSerializable]
public enum ADTGraceOfLazisVisuals : byte
{
    Stage,
}
