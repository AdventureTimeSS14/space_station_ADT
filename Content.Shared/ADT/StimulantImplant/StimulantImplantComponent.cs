using Robust.Shared.Audio;

namespace Content.Shared.ADT.StimulantImplant;

[RegisterComponent]
public sealed partial class StimulantImplantComponent : Component
{
    [DataField]
    public int MaxCharges = 3;

    [DataField]
    public int RemainingCharges = 3;

    [DataField]
    public string Reagent = "ADTBlueSkyDesoxyephedrine";

    [DataField]
    public float Amount = 20f;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");
}
