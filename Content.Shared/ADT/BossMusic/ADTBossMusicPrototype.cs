using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.BossMusic;

[Prototype("adtBossMusic")]
public sealed partial class ADTBossMusicPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    [DataField]
    public float Duck = 0.35f;

    [DataField]
    public float FadeIn = 2f;

    [DataField]
    public float FadeOut = 3f;
}
