using Content.Shared.Chat;
using Robust.Shared.Audio;

namespace Content.Shared.ADT.Chat;


[RegisterComponent]
public sealed partial class TypingSoundComponent : Component
{
    [DataField]
    public SoundSpecifier? TypingSound = new SoundPathSpecifier("/Audio/ADT/Misc/slap.ogg");

    [DataField]
    public SoundSpecifier? MessageSentSound = new SoundPathSpecifier("/Audio/ADT/Misc/slap_secondary.ogg");


}

