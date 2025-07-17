using Robust.Shared.GameStates;

namespace Content.Shared.SD.Cardreader.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class RfidComponent : Component
{
    [DataField("accessLevel"), ViewVariables(VVAccess.ReadWrite)]
    public int AccessLevel = 1; // Уровень доступа (1-5)

    [DataField("limitedUse"), ViewVariables(VVAccess.ReadWrite)]
    public bool LimitedUse = false;

    [DataField("maxUses"), ViewVariables(VVAccess.ReadWrite)]
    public int MaxUses = 5;

    [DataField("usesLeft"), ViewVariables(VVAccess.ReadWrite)]
    public int UsesLeft = 5;

    [DataField]
    public LocId LimitedUseExamineMessage = "rfid-component-examine-limited-uses";
}
