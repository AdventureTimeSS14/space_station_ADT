using Content.Shared.DoAfter;
using Robust.Shared.Audio;

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

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier DoAfterSound = new SoundPathSpecifier("/Audio/ADT/Entities/paper_drop.ogg");
    }
}
