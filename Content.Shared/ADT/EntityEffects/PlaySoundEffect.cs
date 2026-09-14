using Content.Shared.EntityEffects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.EntityEffects.Effects;

/// <summary>
/// Entity effect that plays a sound.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public sealed partial class PlaySoundEffect : EntityEffect
{
    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    [DataField]
    public AudioParams AudioParams = AudioParams.Default;

    public override void RaiseEvent(EntityUid target, IEntityEffectRaiser raiser, float scale, EntityUid? user)
    {
        var ev = new PlaySoundEvent(Sound, AudioParams, target);
        raiser.RaiseEffectEvent(target, ev, scale, user);
    }
}

/// <summary>
/// Event that plays a sound on the target entity.
/// </summary>
public sealed partial class PlaySoundEvent : EntityEffectBase<PlaySoundEvent>
{
    public EntityUid Target;
    public SoundSpecifier Sound;
    public AudioParams AudioParams;

    public PlaySoundEvent(SoundSpecifier sound, AudioParams audioParams, EntityUid target)
    {
        Sound = sound;
        AudioParams = audioParams;
        Target = target;
    }
}