using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

[RegisterComponent]
public sealed partial class AddictionComponent : Component
{
    [DataField("reagents")]
    public Dictionary<string, AddictionType> ReagentAddictions = new();

    public enum AddictionType
    {
        None,
        Tea,
        Drug,
        Tobacco,
        Coffee
    }
}