using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.ADT.Damage.Components;

namespace Content.Shared.Damage
{
    public sealed class SlowOnDamageSystem : EntitySystem
    {
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SlowOnDamageComponent, DamageChangedEvent>(OnDamageChanged);
            SubscribeLocalEvent<SlowOnDamageComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
            SubscribeLocalEvent<IgnoreSlowOnDamageComponent, ComponentStartup>(OnIgnoreStartup); //ADT-Medicine start
            SubscribeLocalEvent<IgnoreSlowOnDamageComponent, ComponentShutdown>(OnIgnoreShutdown);
            SubscribeLocalEvent<IgnoreSlowOnDamageComponent, ModifySlowOnDamageSpeedEvent>(OnIgnoreModifySpeed); //ADT-Medicine end
        }

        private void OnRefreshMovespeed(EntityUid uid, SlowOnDamageComponent component, RefreshMovementSpeedModifiersEvent args)
        {
            if (!EntityManager.TryGetComponent<DamageableComponent>(uid, out var damage))
                return;

            if (damage.TotalDamage == FixedPoint2.Zero)
                return;

            // Get closest threshold
            FixedPoint2 closest = FixedPoint2.Zero;
            var total = damage.TotalDamage;
            foreach (var thres in component.SpeedModifierThresholds)
            {
                if (total >= thres.Key && thres.Key > closest)
                    closest = thres.Key;
            }

            if (closest != FixedPoint2.Zero)
            {
                var speed = component.SpeedModifierThresholds[closest];

                var ev = new ModifySlowOnDamageSpeedEvent(speed); //ADT-Medicine start
                RaiseLocalEvent(uid, ref ev);
                args.ModifySpeed(ev.Speed, ev.Speed); //ADT-Medicine end
            }
        }
        private void OnDamageChanged(EntityUid uid, SlowOnDamageComponent component, DamageChangedEvent args)
        {
            // We -could- only refresh if it crossed a threshold but that would kind of be a lot of duplicated
            // code and this isn't a super hot path anyway since basically only humans have this

            _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(uid);
        }
        private void OnIgnoreStartup(Entity<IgnoreSlowOnDamageComponent> ent, ref ComponentStartup args) //ADT-Medicine start
        {
            _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(ent);
        }

        private void OnIgnoreShutdown(Entity<IgnoreSlowOnDamageComponent> ent, ref ComponentShutdown args)
        {
            _movementSpeedModifierSystem.RefreshMovementSpeedModifiers(ent);
        }

        private void OnIgnoreModifySpeed(Entity<IgnoreSlowOnDamageComponent> ent, ref ModifySlowOnDamageSpeedEvent args)
        {
            args.Speed = 1f;
        }
    }

    [ByRefEvent]
    public record struct ModifySlowOnDamageSpeedEvent(float Speed) : IInventoryRelayEvent
    {
        public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
    } //ADT-Medicine end
}