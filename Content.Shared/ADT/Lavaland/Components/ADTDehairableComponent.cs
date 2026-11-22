using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent]
public sealed partial class ADTDehairableComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Result;

    [DataField]
    public int Multiplier = 1;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);

    [DataField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/Items/Culinary/chop.ogg");
}
