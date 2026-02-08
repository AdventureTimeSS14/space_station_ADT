using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.ADT.BowsSystem.Components;
[RegisterComponent, NetworkedComponent]
public sealed partial class ExpendedBowsComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan coldownStart = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan coldown = TimeSpan.FromSeconds(7f);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int StepOfTension=0;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string ItemSlot="projectiles";

    [DataField]
    public Dictionary<int, string> TensionAndLoc = new Dictionary<int, string>
    {
        {0, "popup-bow-use-null"},
        {1, "popup-bow-use-light"},
        {2, "popup-bow-use-medium"},
        {3, "popup-bow-use-hard"},
    };

    [DataField]
    public Dictionary<int, float> TensionAnModieferSpeed = new Dictionary<int, float>
    {
        {0, 0.5f},
        {1, 1f},
        {2, 1.4f},
        {3, 1.7f},
    };
}