using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Body.Legs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LegMobilityComponent : Component
{
    [DataField, AutoNetworkedField]
    public LegParts Present;

    [DataField]
    public float MissingLegModifier = 0.5f;

    [DataField]
    public float MissingFootModifier = 0.7f;

    [DataField]
    public float CrawlModifier = 0.6f;

    [ViewVariables]
    public bool ForcedDown;
}

[Flags, Serializable, NetSerializable]
public enum LegParts : byte
{
    None = 0,
    LegLeft = 1 << 0,
    LegRight = 1 << 1,
    FootLeft = 1 << 2,
    FootRight = 1 << 3,
}
