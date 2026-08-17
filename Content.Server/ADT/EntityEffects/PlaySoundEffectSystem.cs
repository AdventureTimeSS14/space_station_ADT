using Content.Shared.ADT.EntityEffects.Effects;
using Content.Shared.EntityEffects;
using Robust.Shared.Audio.Systems;

namespace Content.Server.ADT.EntityEffects.Effects;

/// <summary>
/// System that handles PlaySoundEvent.
/// </summary>
public sealed partial class PlaySoundEffectSystem : EntityEffectSystem<TransformComponent, PlaySoundEvent>
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<PlaySoundEvent> args)
    {
        var uid = entity.Owner;
        var ev = args.Effect;

        _audio.PlayPredicted(ev.Sound, Transform(uid).Coordinates, uid, ev.AudioParams);
    }
}