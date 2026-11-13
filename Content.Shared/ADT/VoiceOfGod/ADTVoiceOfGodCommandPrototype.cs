using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.VoiceOfGod;

[Prototype("adtVoiceOfGodCommand")]
public sealed partial class ADTVoiceOfGodCommandPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public int Priority;

    [DataField(required: true)]
    public List<string> Words = new();

    [DataField]
    public ADTVoiceOfGodCooldown Cooldown = ADTVoiceOfGodCooldown.Meme;

    [DataField]
    public EntityCondition[]? UserConditions;

    [DataField]
    public ADTVoiceOfGodCooldown FailCooldown = ADTVoiceOfGodCooldown.Meme;

    [DataField]
    public List<ADTVoiceOfGodPowerModifier> PowerModifiers = new();

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public EntityEffect[] Effects = Array.Empty<EntityEffect>();
}

[DataDefinition]
public sealed partial class ADTVoiceOfGodPowerModifier
{
    [DataField(required: true)]
    public EntityCondition[] AnyOf = Array.Empty<EntityCondition>();

    [DataField(required: true)]
    public float Multiplier = 1f;
}

public enum ADTVoiceOfGodCooldown : byte
{
    Stun,
    Damage,
    Meme,
    None,
}
