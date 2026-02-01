// ЕСТЬ аналог Content.Shared\Trigger\Systems\DamageOnTriggerSystem.cs

// // Simple  Station

// using Content.Server.Damage.Components;
// using Content.Server.Explosion.EntitySystems;
// using Content.Shared.Damage;
// using Content.Shared.Damage.Systems;
// using Content.Shared.StepTrigger.Systems;
// using Content.Shared.Trigger;

// namespace Content.Server.Damage.Systems
// {
//     // System for damage that occurs on specific trigger, towards the entity..
//     public sealed class DamageOnTriggerSystem : EntitySystem
//     {
//         [Dependency] private readonly DamageableSystem _damageableSystem = default!;

//         public override void Initialize()
//         {
//             base.Initialize();
//             SubscribeLocalEvent<DamageOnTriggerComponent, TriggerEvent>(DamageOnTrigger);
//         }

//         private void DamageOnTrigger(EntityUid uid, DamageOnTriggerComponent component, TriggerEvent args)
//         {
//             _damageableSystem.TryChangeDamage(uid, component.Damage, component.IgnoreResistances);
//         }
//     }
// }
