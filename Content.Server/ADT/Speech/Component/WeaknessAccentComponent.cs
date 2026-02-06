namespace Content.Server.ADT.Speech.Components
{
    [RegisterComponent]
    public sealed partial class WeaknessAccentComponent : Component
    {
        /// <summary>
        /// Percentage chance that an ellipsis will occur between words.
        /// </summary>
        [DataField("matchRandomProb")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float MatchRandomProb = 0.5f;
    }
}
