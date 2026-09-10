using Content.Shared.Damage.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Shared.Mobs.Systems;

// ADT-Tweak-start
public partial class MobStateSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private readonly Dictionary<EntityUid, EntityUid> _stateAudio = new();

    private const float SoftCritImmobileDamage = 150f;
    private const float SoftCritSpeedModifier = 0.35f;

    private static readonly Dictionary<MobState, (string Sound, bool Loop, float Volume)> StateAudio = new()
    {
        { MobState.SoftCritical, ("/Audio/ADT/Effects/soft_critical.ogg", true, -6f) },
        { MobState.Critical, ("/Audio/ADT/Effects/critical.ogg", true, -8f) },
        { MobState.Alive, ("/Audio/ADT/Effects/backtolife.ogg", false, -4f) },
    };

    private void OnUpdateCanMove(EntityUid uid, MobStateComponent component, ref UpdateCanMoveEvent args)
    {
        if (component.CurrentState == MobState.SoftCritical)
        {
            if (TryComp<DamageableComponent>(uid, out var damage)
                && _damageable.GetTotalDamage((uid, damage)) > SoftCritImmobileDamage)
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

        args.ModifySpeed(SoftCritSpeedModifier, SoftCritSpeedModifier);
    }

    private void PlayStateAudio(EntityUid uid, MobState state)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!StateAudio.TryGetValue(state, out var data))
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

        _stateAudio[uid] = audio.Value.Entity;
    }

    private void StopStateAudio(EntityUid uid)
    {
        if (!_stateAudio.Remove(uid, out var audio))
            return;

        if (Exists(audio))
            QueueDel(audio);
    }
}
// ADT-Tweak-end
