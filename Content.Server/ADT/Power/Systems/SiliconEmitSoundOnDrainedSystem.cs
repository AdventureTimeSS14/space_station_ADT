// Simple Station

using Content.Server.ADT.Silicon.Death;
using Content.Shared.Sound.Components;
using Content.Shared.Mobs;
using Robust.Shared.Timing;
//using Content.Shared.SimpleStation14.Silicon.Systems;

namespace Content.Server.ADT.Silicon;

public sealed class EmitSoundOnCritSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<SiliconEmitSoundOnDrainedComponent, SiliconChargeDeathEvent>(OnDeath);
        SubscribeLocalEvent<SiliconEmitSoundOnDrainedComponent, SiliconChargeAliveEvent>(OnAlive);
        SubscribeLocalEvent<SiliconEmitSoundOnDrainedComponent, MobStateChangedEvent>(OnStateChange);
    }

    private void OnDeath(EntityUid uid, SiliconEmitSoundOnDrainedComponent component, SiliconChargeDeathEvent args)
    {
        var spamComp = EnsureComp<SpamEmitSoundComponent>(uid);

        // spamComp.Accumulator = 0f;
        spamComp.MinInterval = TimeSpan.FromSeconds(component.Interval);
        spamComp.MaxInterval = TimeSpan.FromSeconds(component.Interval);
        spamComp.PopUp = component.PopUp;
        spamComp.Enabled = true;
        spamComp.Sound = component.Sound;
    }

    private void OnAlive(EntityUid uid, SiliconEmitSoundOnDrainedComponent component, SiliconChargeAliveEvent args)
    {
        RemComp<SpamEmitSoundComponent>(uid); // This component is bad and I don't feel like making a janky work around because of it.
        // If you give something the SiliconEmitSoundOnDrainedComponent, know that it can't have the SpamEmitSoundComponent, and any other systems that play with it will just be broken.
    }

    public void OnStateChange(EntityUid uid, SiliconEmitSoundOnDrainedComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            RemComp<SpamEmitSoundComponent>(uid);
    }
}
