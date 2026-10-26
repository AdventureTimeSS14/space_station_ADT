//

using Content.Shared.FixedPoint;
using Content.Shared.ADT.Heretic.Common;
using Content.Shared.ADT.MartialArts;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.ADT.Heretic.Components;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Cloning;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Heretic;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition;
using Content.Shared.NPC.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem
{
    private static readonly ProtoId<CloningSettingsPrototype> Settings = "FleshMimic";
    private static readonly SoundSpecifier MimicSpawnSound = new SoundCollectionSpecifier("gib");

    protected override void SubscribeFlesh()
    {
        base.SubscribeFlesh();

        SubscribeLocalEvent<FleshPassiveComponent, DamageChangedEvent>(OnDamageChanged);
        // ADT: no shitmed stomach, hook core ingest event
        SubscribeLocalEvent<FleshPassiveComponent, IngestingEvent>(OnConsumingFood);
    }

    private void OnConsumingFood(Entity<FleshPassiveComponent> ent, ref IngestingEvent args)
    {
        if (args.Split.Volume <= FixedPoint2.Zero)
            return;

        if (!Heretic.TryGetHereticComponent(ent, out var heretic, out _) || heretic.PathStage <= 0)
            return;

        var multiplier = GetMultiplier((ent.Owner, ent.Comp), heretic, ref args, out var stage, out var multipliersApplied);
        if (!multipliersApplied)
            return;

        var time = TimeSpan.FromMinutes(1) * stage;
        if (heretic.Ascended)
            time += TimeSpan.FromMinutes(1);

        ApplyMultiplier(ent, multiplier * ent.Comp.BaseHealingPerFlesh, time, MartialArtModifierType.Healing);
        ApplyMultiplier(ent, multiplier * ent.Comp.BaseAttackRatePerFlesh, time, MartialArtModifierType.AttackRate);
        ApplyMultiplier(ent, multiplier * ent.Comp.BaseMoveSpeedPerFlesh, time, MartialArtModifierType.MoveSpeed);
        _modifier.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private float GetMultiplier(Entity<FleshPassiveComponent> ent,
        HereticComponent heretic,
        ref IngestingEvent args,
        out float stage,
        out bool multipliersApplied)
    {
        stage = MathF.Pow(heretic.PathStage, 0.3f);
        var multiplier = args.Split.Volume.Float() * stage;
        var oldMult = multiplier;

        if (HasComp<MobStateComponent>(args.Food))
            multiplier *= ent.Comp.MobMultiplier;
        if (HasComp<BrainComponent>(args.Food))
            multiplier *= ent.Comp.BrainMultiplier;
        // ADT: no BodyPartComponent (shitmed), no limb multiplier
        if (HasComp<OrganComponent>(args.Food))
            multiplier *= ent.Comp.OrganMultiplier;
        if (HasComp<HumanOrganComponent>(args.Food))
            multiplier *= ent.Comp.HumanMultiplier;
        if (_tag.HasTag(args.Food, ent.Comp.MeatTag))
            multiplier *= ent.Comp.MeatMultiplier;
        if (heretic.Ascended)
            multiplier *= ent.Comp.AscensionMultiplier;

        multipliersApplied = oldMult < multiplier;
        return multiplier;
    }

    // Martial arts cuz yeah
    private void ApplyMultiplier(EntityUid uid, float multiplier, TimeSpan time, MartialArtModifierType type)
    {
        if (Math.Abs(multiplier) < 0.01f || time <= TimeSpan.Zero)
            return;

        var multComp = EnsureComp<MartialArtModifiersComponent>(uid);
        multComp.Data.Add(new MartialArtModifierData
        {
            Type = type,
            Multiplier = multiplier + 1f,
            EndTime = Timing.CurTime + time,
        });

        Dirty(uid, multComp);
    }

    private void OnDamageChanged(Entity<FleshPassiveComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased || args.DamageDelta == null)
            return;

        if (_mobstate.IsDead(ent))
            return;

        var damage = args.DamageDelta.GetTotal();

        if (damage <= 0)
            return;

        if (!Heretic.TryGetHereticComponent(ent, out var heretic, out _) || !heretic.Ascended)
            return;

        ent.Comp.TrackedDamage += damage;

        ent.Comp.FleshMimics.RemoveAll(x => !Exists(x));

        if (ent.Comp.MaxMimics <= ent.Comp.FleshMimics.Count)
        {
            var toHeal = -ent.Comp.TrackedDamage / ent.Comp.FleshMimics.Count * ent.Comp.MimicHealMultiplier;
            ent.Comp.TrackedDamage = FixedPoint2.Zero;
            foreach (var mimic in ent.Comp.FleshMimics)
            {
                IHateWoundMed(mimic, AllDamage * toHeal, toHeal, toHeal, toHeal, null, null);
            }

            return;
        }

        var maxToSpawn = ent.Comp.MaxMimics - ent.Comp.FleshMimics.Count;
        var toSpawn = (int) (ent.Comp.TrackedDamage / ent.Comp.MimicDamage);
        toSpawn = Math.Clamp(toSpawn, 0, maxToSpawn);

        if (toSpawn == 0)
            return;

        for (var i = 0; i < toSpawn; i++)
        {
            if (CreateFleshMimic(ent, ent, true, true, 50, args.Origin) is { } clone)
                ent.Comp.FleshMimics.Add(clone);
        }

        ent.Comp.TrackedDamage -= toSpawn * ent.Comp.MimicDamage;
    }

    public EntityUid? CreateFleshMimic(EntityUid uid,
        EntityUid user,
        bool giveBlade,
        bool makeGhostRole,
        FixedPoint2 hp,
        EntityUid? hostile)
    {
        if (_mobstate.IsDead(uid) || HasComp<GhoulComponent>(uid) || HasComp<BorgChassisComponent>(uid))
            return null;

        var xform = Transform(uid);
        if (!_cloning.TryCloning(uid, _xform.GetMapCoordinates(xform), Settings, out var clone))
            return null;

        _aud.PlayPvs(MimicSpawnSound, xform.Coordinates);

        MarkMimicEquipmentUnremoveable(clone.Value);

        EntityUid? weapon = null;
        if (!giveBlade && TryComp(uid, out HandsComponent? hands))
        {
            foreach (var held in _hands.EnumerateHeld((uid, hands)))
            {
                if (HasComp<GunComponent>(held))
                {
                    weapon = held;
                    break;
                }

                if (HasComp<MeleeWeaponComponent>(held) && weapon == null)
                    weapon = held;
            }
        }

        var minion = EnsureComp<HereticMinionComponent>(clone.Value);
        minion.BoundHeretic = user;
        Dirty(clone.Value, minion);

        var ghoul = _compFactory.GetComponent<GhoulComponent>();
        ghoul.GiveBlade = giveBlade;
        ghoul.TotalHealth = hp;
        ghoul.DropOrgansOnDeath = false;
        ghoul.GhostRoleName = "ghostrole-flesh-mimic-name";
        ghoul.GhostRoleDesc = "ghostrole-flesh-mimic-desc";
        if (weapon != null && _cloning.CopyItem(weapon.Value, xform.Coordinates) is { } weaponClone)
        {
            if (!_hands.TryPickup(clone.Value, weaponClone, null, false, false, false))
                QueueDel(weaponClone);
            else
            {
                EnsureComp<GhoulWeaponComponent>(weaponClone);
                ghoul.BoundWeapon = weaponClone;
                MarkItemAndStorageUnremoveable(weaponClone);
            }
        }

        AddComp(clone.Value, ghoul);

        if (_statusEffect.TryGetTime(uid, "KnockedDown", out var knockdownStartEnd))
        {
            var time = knockdownStartEnd.Value.Item2 - Timing.CurTime;
            if (time > TimeSpan.Zero)
                _stun.TryKnockdown(clone.Value, time, true, true, false);
        }

        var damage = EnsureComp<DamageOverTimeComponent>(clone.Value);
        damage.Damage = new DamageSpecifier
        {
            DamageDict =
            {
                { "Blunt", 0.3 },
                { "Slash", 0.3 },
                { "Piercing", 0.3 },
            }
        };
        damage.MultiplierIncrease = 0.02f;
        damage.IgnoreResistances = true;
        Dirty(clone.Value, damage);

        if (!makeGhostRole)
            RemCompDeferred<GhostTakeoverAvailableComponent>(clone.Value);
        else if (TryComp(clone.Value, out GhostRoleComponent? ghostRole))
            ghostRole.RaffleConfig = null;

        var exception = EnsureComp<FactionExceptionComponent>(clone.Value);
        _npcFaction.IgnoreEntity((clone.Value, exception), user);
        if (user != uid)
        {
            _npcFaction.AggroEntity((clone.Value, exception), uid);
            EnsureComp<FleshMimickedComponent>(uid).FleshMimics.Add(clone.Value);
        }
        if (hostile != null && hostile.Value != user)
        {
            _npcFaction.AggroEntity((clone.Value, exception), hostile.Value);
            EnsureComp<FleshMimickedComponent>(hostile.Value).FleshMimics.Add(clone.Value);
        }

        return clone.Value;
    }

    private void MarkMimicEquipmentUnremoveable(EntityUid mimic)
    {
        if (TryComp(mimic, out InventoryComponent? inventory))
        {
            var slots = _inventory.GetSlotEnumerator((mimic, inventory));
            while (slots.NextItem(out var item, out _))
            {
                MarkItemAndStorageUnremoveable(item);
            }
        }

        if (TryComp(mimic, out HandsComponent? hands))
        {
            foreach (var held in _hands.EnumerateHeld((mimic, hands)))
            {
                MarkItemAndStorageUnremoveable(held);
            }
        }
    }

    private void MarkItemAndStorageUnremoveable(EntityUid item)
    {
        EnsureComp<UnremoveableComponent>(item);

        if (!TryComp(item, out ContainerManagerComponent? containerManager))
            return;

        var cartridgeQuery = GetEntityQuery<CartridgeAmmoComponent>();
        foreach (var container in containerManager.Containers.Values)
        {
            foreach (var contained in container.ContainedEntities)
            {
                if (cartridgeQuery.HasComp(contained))
                    continue;

                MarkItemAndStorageUnremoveable(contained);
            }
        }
    }
}
