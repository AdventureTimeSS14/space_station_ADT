using Robust.Shared.Audio;

namespace Content.Shared.ADT.BloodPumpImplant;

[RegisterComponent]
public sealed partial class BloodPumpImplantComponent : Component
{
    [DataField]
    public int MaxCharges = 3;

    [DataField]
    public int RemainingCharges = 3;

    [DataField]
    public Dictionary<string, float> Reagents = new()
    {
        ["Puncturase"] = 5f,
        ["ADTStypticPowder"] = 15f,
        ["Saline"] = 10f,
    };

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");
}
