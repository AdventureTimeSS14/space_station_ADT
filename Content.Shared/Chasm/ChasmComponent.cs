using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Chasm;

/// <summary>
///     Marks a component that will cause entities to fall into them on a step trigger activation
/// </summary>
[NetworkedComponent, RegisterComponent, Access(typeof(ChasmSystem))]
public sealed partial class ChasmComponent : Component
{
    //ADT-Tweak-Start
    public SoundSpecifier FallingSound = new SoundPathSpecifier("/Audio/Effects/falling.ogg");
    [DataField("PendingFallSound")]
    public SoundSpecifier PendingFallSound = new SoundPathSpecifier("/Audio/ADT/SoundGen/destruction_2.ogg");
    //ADT-Tweak-End
}
