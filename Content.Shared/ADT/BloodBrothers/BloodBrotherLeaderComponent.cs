using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Shared.ADT.BloodBrothers
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class BloodBrotherLeaderComponent : Component
    {
        /// <summary>
        /// The status icon prototype displayed for brothers
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "BloodBrotherLeaderFaction";
        /// <summary>
        /// Sound that plays when you are chosen. (Placeholder until I find something cool I guess)
        /// </summary>
        [DataField]
        public SoundSpecifier StartSound = new SoundCollectionSpecifier("ADTTraitorStart");

        public int ConvertedCount = 0;
        [DataField]
        public int MaxConvertedCount = 4;
    }
}


