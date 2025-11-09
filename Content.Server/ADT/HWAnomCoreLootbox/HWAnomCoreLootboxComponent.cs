using Content.Shared.DoAfter;

namespace Content.Server.ADT.HWAnomCoreLootbox
{
    /// <summary>
    ///     Spawns items when used in hand.
    /// </summary>
    [RegisterComponent]
    public sealed partial class HWAnomCoreLootboxComponent : Component
    {
        [DataDefinition]
        public partial struct HWAnomCoreLootboxSettings
        {
            [DataField]
            public float Duration = 10f;
            [DataField]
            public float UseDelay;
        }
        [DataField, ViewVariables]
        public HWAnomCoreLootboxSettings Settings = new();
        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public DoAfterId? DoAfter;
    }
}
