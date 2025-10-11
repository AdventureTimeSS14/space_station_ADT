using Robust.Shared.GameStates;
using Content.Shared.Antag;
using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Shared.ADT.BloodBrothers
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class BloodBrotherComponent : Component
    {
        /// <summary>
        /// The status icon prototype displayed for brothers
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "BloodBrotherFaction";
    }
}


