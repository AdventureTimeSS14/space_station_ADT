using Robust.Shared.Serialization;
using Robust.Shared.Audio;

namespace Content.Shared.Paper;

/// <summary>
///     Set of required information to draw a stamp in UIs, where
///     representing the state of the stamp at the point in time
///     when it was applied to a paper. These fields mirror the
///     equivalent in the component.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public partial struct StampDisplayInfo
{
    // ADT-Tweak: Start
    // StampDisplayInfo(string s)
    // {
    //     StampedName = s;
    // }
    // ADT-Tweak: End
    [DataField("stampedName")]
    public string StampedName;

    [DataField("stampedColor")]
    public Color StampedColor;
    // ADT-Tweak: Start
    // Stamp type.
    [DataField("type")]
    public StampType Type = StampType.RubberStamp;

    [DataField("font")]
    public string? Font { get; set; } = "/Fonts/NotoSans/NotoSans-Regular.ttf"; // Default font :3
};

// Stamp "types". Currently, rubber stamp and signatures.
public enum StampType
{
    RubberStamp,
    Signature
}
// ADT-Tweak: End
[RegisterComponent]
public sealed partial class StampComponent : Component
{
    /// <summary>
    ///     The loc string name that will be stamped to the piece of paper on examine.
    /// </summary>
    [DataField("stampedName")]
    public string StampedName { get; set; } = "stamp-component-stamped-name-default";

    /// <summary>
    ///     The sprite state of the stamp to display on the paper from paper Sprite path.
    /// </summary>
    [DataField("stampState")]
    public string StampState { get; set; } = "paper_stamp-generic";

    /// <summary>
    /// The color of the ink used by the stamp in UIs
    /// </summary>
    [DataField("stampedColor")]
    public Color StampedColor = Color.FromHex("#BB3232"); // StyleNano.DangerousRedFore

    /// <summary>
    /// The sound when stamp stamped
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound = null;
}
