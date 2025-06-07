// using Content.Shared.Projectiles;
// using Content.Shared.Weapons.Ranged.Events;
// using Content.Shared.Weapons.Ranged.Upgrades.Components;
// using Content.Server.Projectiles.Components;
// using Robust.Shared.GameObjects;

// namespace Content.Server.ADT.Projectile.Systems
// {
//     public sealed class ProjectileIgnoreCrawlingTransferSystem : EntitySystem
//     {
//         public override void Initialize()
//         {
//             SubscribeLocalEvent<UpgradeableGunComponent, GunShotEvent>(OnTransferIgnoreCrawling);
//         }

//         private void OnTransferIgnoreCrawling(EntityUid uid, UpgradeableGunComponent component, ref GunShotEvent args)
//         {
//             if (!HasComp<ProjectileIgnoreCrawlingComponent>(args.User))
//                 return;

//             foreach (var (ammo, _) in args.Ammo)
//             {
//                 if (!HasComp<ProjectileComponent>(ammo))
//                     continue;

//                 EnsureComp<ProjectileIgnoreCrawlingComponent>(ammo.Value);
//             }
//         }
//     }
// }