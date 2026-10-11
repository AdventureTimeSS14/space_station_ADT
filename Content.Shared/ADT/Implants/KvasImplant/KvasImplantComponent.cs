using Robust.Shared.Audio;

namespace Content.Shared.ADT.Implants.KvasImplant;

[RegisterComponent]
public sealed partial class KvasImplantComponent : Component
{
    [DataField]
    public string Reagent = "Kvass";

    [DataField]
    public float Amount = 15f;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Items/hypospray.ogg");
}
