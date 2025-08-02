using Robust.Shared.Audio;

namespace Content.Server.ADT.Terraformer;

[RegisterComponent]
public sealed partial class TerraformerComponent : Component
{
    [DataField]
    public string ActivationCode;

    [DataField]
    public SoundSpecifier DenySound;

    public const string ContainerId = "disk_slot";
}
