using Content.Shared.Timing;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Upgrades.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.ADT.Crawling.Components;

namespace Content.Server.ADT.Projectile.Systems
{
    public sealed class ProjectileIgnoreCrawlingTransferSystem : EntitySystem
    {
        public override void Initialize()
        {
            SubscribeLocalEvent<ProjectileIgnoreCrawlingComponent, GunShotEvent>(OnTransferIgnoreCrawling);
        }

        private void OnTransferIgnoreCrawling(Entity<ProjectileIgnoreCrawlingComponent> ent, ref GunShotEvent args)
        {
            if (!HasComp<ProjectileIgnoreCrawlingComponent>(args.User))
                return;

            foreach (var (ammo, _) in args.Ammo)
            {
                if (!HasComp<ProjectileComponent>(ammo))
                    continue;

                EnsureComp<ProjectileIgnoreCrawlingComponent>(ammo.Value);
            }
        }
    }
}