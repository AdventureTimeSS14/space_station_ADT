using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Cooking.Components;

[RegisterComponent]
public sealed partial class ADTCookwareCrackableComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Result;

    [DataField]
    public EntProtoId? Trash;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public HashSet<ADTCookwareType> Cookware = new();
}
