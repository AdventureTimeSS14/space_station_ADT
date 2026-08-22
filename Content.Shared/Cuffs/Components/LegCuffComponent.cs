using Robust.Shared.Audio;

namespace Content.Shared.Cuffs.Components;

[RegisterComponent]
public sealed partial class LegCuffComponent : Component
{
    [DataField]
    public string CuffedRSI = "ADT/Objects/Misc/legcuffs.rsi";

    [DataField]
    public string BodyIconState = "leg-irons";

    [DataField]
    public SoundSpecifier StartCuffSound = new SoundPathSpecifier("/Audio/ADT/Entities/Objects/handcuffs.ogg");

    [DataField]
    public SoundSpecifier RemoveCuffSound = new SoundPathSpecifier("/Audio/ADT/Entities/Objects/handcuffs.ogg");

    [DataField]
    public float ApplyDelay = 4f;
}
