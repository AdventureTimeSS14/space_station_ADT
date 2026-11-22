using Robust.Shared.Audio;

namespace Content.Server.ADT.LinkedBodies;

[RegisterComponent, Access(typeof(ADTLinkedBodiesSystem))]
public sealed partial class ADTLinkedBodiesComponent : Component
{
    [ViewVariables]
    public Dictionary<EntityUid, EntityUid> Actions = new();

    [DataField]
    public SoundSpecifier SwapSound = new SoundPathSpecifier("/Audio/Magic/blink.ogg");
}
