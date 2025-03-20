using Content.Server.DeviceLinking.Events;
using Content.Server.DeviceLinking.Systems;
using Content.Server.Explosion.Components;

namespace Content.Server.Explosion.EntitySystems
{
    public sealed partial class TriggerSystem
    {
        [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
        private void InitializeSignal()
        {
            SubscribeLocalEvent<TriggerOnSignalComponent,SignalReceivedEvent>(OnSignalReceived);
            SubscribeLocalEvent<TriggerOnSignalComponent,ComponentInit>(OnInit);

/* (Откат PR - https://github.com/space-wizards/space-station-14/pull/32423)
            SubscribeLocalEvent<TimerStartOnSignalComponent,SignalReceivedEvent>(OnTimerSignalReceived);
            SubscribeLocalEvent<TimerStartOnSignalComponent,ComponentInit>(OnTimerSignalInit);
*/        
        }

        private void OnSignalReceived(EntityUid uid, TriggerOnSignalComponent component, ref SignalReceivedEvent args)
        {
            if (args.Port != component.Port)
                return;

            Trigger(uid, args.Trigger);
        }
        private void OnInit(EntityUid uid, TriggerOnSignalComponent component, ComponentInit args)
        {
            _signalSystem.EnsureSinkPorts(uid, component.Port);
        }

/* (Откат PR - https://github.com/space-wizards/space-station-14/pull/32423)
        private void OnTimerSignalReceived(EntityUid uid, TimerStartOnSignalComponent component, ref SignalReceivedEvent args)
        {
            if (args.Port != component.Port)
                return;

            StartTimer(uid, args.Trigger);
        }
        private void OnTimerSignalInit(EntityUid uid, TimerStartOnSignalComponent component, ComponentInit args)
        {
            _signalSystem.EnsureSinkPorts(uid, component.Port);
        }
*/
    }
}
