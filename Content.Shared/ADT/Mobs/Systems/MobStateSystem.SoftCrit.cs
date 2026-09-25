using Content.Shared.ADT.Mobs;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Shared.Mobs.Systems;

public partial class MobStateSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private void OnUpdateCanMove(EntityUid uid, MobStateComponent component, ref UpdateCanMoveEvent args)
    {
        if (component.CurrentState == MobState.SoftCritical)
        {
            if (TryComp<SoftCritComponent>(uid, out var softCrit)
                && TryComp<DamageableComponent>(uid, out var damage)
                && _damageable.GetTotalDamage((uid, damage)) > softCrit.ImmobileDamageThreshold)
            {
                args.Cancel();
            }

            return;
        }

        CheckAct(uid, component, args);
    }

    private void OnSoftCritSpeed(EntityUid uid, MobStateComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.CurrentState != MobState.SoftCritical)
            return;

        if (!TryComp<SoftCritComponent>(uid, out var softCrit))
            return;

        args.ModifySpeed(softCrit.SpeedModifier, softCrit.SpeedModifier);
    }

    private void PlayStateAudio(EntityUid uid, MobState state)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryComp<SoftCritComponent>(uid, out var softCrit))
            return;

        if (!SoftCritComponent.StateAudio.TryGetValue(state, out var data))
            return;

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        StopStateAudio(uid);

        var audio = _audio.PlayEntity(
            new SoundPathSpecifier(data.Sound),
            Filter.SinglePlayer(actor.PlayerSession),
            uid,
            false,
            new AudioParams { Loop = data.Loop, Volume = data.Volume });

        if (audio == null)
            return;

        softCrit.StateAudioEntity = audio.Value.Entity;
    }

    private void StopStateAudio(EntityUid uid)
    {
        if (!TryComp<SoftCritComponent>(uid, out var softCrit))
            return;

        var audio = softCrit.StateAudioEntity;
        softCrit.StateAudioEntity = null;

        if (audio == null)
            return;

        if (Exists(audio.Value))
            QueueDel(audio.Value);
    }
}
