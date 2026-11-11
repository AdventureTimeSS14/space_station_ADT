using System.Linq;
using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.ADT.Virology;
using Content.Shared.ADT.Virology.Components;
using Content.Shared.ADT.Virology.Prototypes;
using Content.Shared.Armor;
using Content.Shared.Atmos;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.ADT.Virology;

public sealed partial class ADTDiseaseSystem
{
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private SharedArmorSystem _armor = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedInternalsSystem _internals = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public const string ZoneHead = "head";
    public const string ZoneChest = "chest";
    public const string ZoneArms = "arms";
    public const string ZoneHands = "hands";
    public const string ZoneLegs = "legs";
    public const string ZoneFeet = "feet";

    private static readonly string[] ProtectionSlots =
    {
        "head", "mask", "eyes", "outerClothing", "jumpsuit", "gloves", "shoes", "neck",
    };

    private void InitializeSpread()
    {
        SubscribeLocalEvent<ADTDiseaseCarrierComponent, ContactInteractionEvent>(OnContactInteraction);
        SubscribeLocalEvent<ADTDiseaseCarrierComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private bool CanSpread(EntityUid uid, ADTDisease disease, ADTDiseasePrototype proto, bool dead)
    {
        if (IsContained(uid))
            return false;

        if (!_random.Prob(proto.Infectivity / 100f))
            return false;

        if (dead && !_random.Prob(proto.SpreadFromDeadProb / 100f))
            return false;

        return true;
    }

    public bool IsContained(EntityUid uid)
    {
        if (!_container.TryGetContainingContainer(uid, out var container))
            return false;

        return HasComp<ADTDiseaseContainmentComponent>(container.Owner);
    }

    public void Spread(EntityUid source, ADTDisease disease, int forceSpread = 0)
    {
        if ((disease.Spread & (ADTDiseaseSpread.Contact | ADTDiseaseSpread.Airborne)) == 0 && forceSpread == 0)
            return;

        if (HasReagent(source, Spaceacillin))
            return;

        var range = forceSpread > 0 ? forceSpread : 1;
        if ((disease.Spread & ADTDiseaseSpread.Airborne) != 0)
            range++;

        var xform = Transform(source);
        if (xform.GridUid is not { } gridUid || !TryComp<MapGridComponent>(gridUid, out var grid))
            return;

        var sourceTile = _map.TileIndicesFor(gridUid, grid, xform.Coordinates);
        var actType = range == 1
            ? ADTDiseaseSpread.Contact
            : ADTDiseaseSpread.Contact | ADTDiseaseSpread.Airborne;

        var targets = new HashSet<Entity<MobStateComponent>>();
        _lookup.GetEntitiesInRange(xform.Coordinates, range + 0.5f, targets, LookupFlags.Dynamic | LookupFlags.Sundries);

        foreach (var victim in targets)
        {
            if (victim.Owner == source)
                continue;

            var victimXform = Transform(victim);
            if (victimXform.GridUid != gridUid)
                continue;

            var victimTile = _map.TileIndicesFor(gridUid, grid, victimXform.Coordinates);
            var delta = sourceTile - victimTile;
            if (Math.Max(Math.Abs(delta.X), Math.Abs(delta.Y)) > range)
                continue;

            if (!HasAirPath(gridUid, grid, victimTile, sourceTile))
                continue;

            if (CheckVirusProtection(source, disease, actType, null))
                return;

            Contract(victim, disease, actType, protectionCheck: true);
        }
    }

    private bool HasAirPath(EntityUid gridUid, MapGridComponent grid, Vector2i from, Vector2i to)
    {
        var current = from;
        var steps = 0;
        while (current != to && steps < 16)
        {
            steps++;
            var diff = to - current;
            var step = Math.Abs(diff.X) >= Math.Abs(diff.Y)
                ? new Vector2i(Math.Sign(diff.X), 0)
                : new Vector2i(0, Math.Sign(diff.Y));

            var dir = StepToAtmosDirection(step);
            var next = current + step;
            if (_atmos.IsTileAirBlocked(gridUid, current, dir, grid))
                return false;

            if (_atmos.IsTileAirBlocked(gridUid, next, dir.GetOpposite(), grid))
                return false;

            current = next;
        }

        return current == to;
    }

    private static AtmosDirection StepToAtmosDirection(Vector2i step)
    {
        if (step.X > 0)
            return AtmosDirection.East;

        if (step.X < 0)
            return AtmosDirection.West;

        if (step.Y > 0)
            return AtmosDirection.North;

        return AtmosDirection.South;
    }

    public bool CheckVirusProtection(EntityUid uid, ADTDisease disease, ADTDiseaseSpread actType, string? zone)
    {
        if (actType == ADTDiseaseSpread.None)
            actType = ADTDiseaseSpread.Bites | ADTDiseaseSpread.Contact | ADTDiseaseSpread.Airborne;

        if (_random.Prob(Math.Clamp(15f / disease.Permeability, 0f, 100f) / 100f))
            return true;

        if ((actType & ADTDiseaseSpread.Bites) != 0 && !CheckBitesProtection(uid, disease))
            return false;

        if ((actType & ADTDiseaseSpread.Contact) != 0 && !CheckContactProtection(uid, zone))
            return false;

        if ((actType & ADTDiseaseSpread.Airborne) != 0 && !CheckAirborneProtection(uid, disease))
            return false;

        return true;
    }

    private bool CheckBitesProtection(EntityUid uid, ADTDisease disease)
    {
        var coefficient = 1f;
        var enumerator = _inventory.GetSlotEnumerator(uid);
        while (enumerator.NextItem(out var item))
        {
            coefficient *= _armor.GetArmorCoefficient(item, "Blunt");
        }

        var armorValue = Math.Clamp((1f - coefficient) * 100f, 0f, 100f);
        return _random.Prob(Math.Clamp(armorValue / disease.Permeability, 0f, 100f) / 100f);
    }

    private bool CheckContactProtection(EntityUid uid, string? zone)
    {
        if (!HasComp<HumanoidProfileComponent>(uid))
            return false;

        zone ??= PickContactZone();

        foreach (var slot in ProtectionSlots)
        {
            if (!_inventory.TryGetSlotEntity(uid, slot, out var item))
                continue;

            if (!TryComp<ADTVirusProtectionComponent>(item, out var protection))
                continue;

            if (!protection.Zones.Contains(zone))
                continue;

            if (_random.Prob(Math.Clamp(1f - protection.Permeability, 0f, 1f)))
                return true;
        }

        return false;
    }

    private string PickContactZone()
    {
        var roll = _random.Next(100);
        if (roll < 40)
            return ZoneHead;

        if (roll < 80)
            return ZoneChest;

        if (roll < 90)
            return ZoneArms;

        return ZoneLegs;
    }

    private bool CheckAirborneProtection(EntityUid uid, ADTDisease disease)
    {
        if (!HasComp<HumanoidProfileComponent>(uid))
            return false;

        var internalsMod = TryComp<InternalsComponent>(uid, out var internals) && _internals.AreInternalsWorking(uid, internals)
            ? 1f
            : 0.2f;

        var permeabilityMod = Math.Clamp(2f - disease.Permeability, 0.1f, 1f);
        var maskMod = 1f;

        if (_inventory.TryGetSlotEntity(uid, "mask", out var mask))
        {
            if (TryComp<ADTVirusProtectionComponent>(mask, out var protection) && protection.Airborne is { } airborne)
                maskMod = airborne;
            else if (TryComp<IngestionBlockerComponent>(mask, out var blocker) && blocker.Enabled)
                maskMod = 0.5f;
        }

        return _random.Prob(Math.Clamp(permeabilityMod * internalsMod * maskMod, 0f, 1f));
    }

    private void OnContactInteraction(Entity<ADTDiseaseCarrierComponent> ent, ref ContactInteractionEvent args)
    {
        var other = args.Other;
        if (!HasComp<MobStateComponent>(other))
            return;

        foreach (var disease in ent.Comp.Diseases.ToList())
        {
            if (!_proto.TryIndex(disease.Prototype, out var proto) || !proto.IsVirus)
                continue;

            if ((disease.Spread & ADTDiseaseSpread.Contact) == 0)
                continue;

            Contract(other, disease, ADTDiseaseSpread.Contact, protectionCheck: true);
        }
    }

    private void OnMeleeHit(Entity<ADTDiseaseCarrierComponent> ent, ref MeleeHitEvent args)
    {
        if (args.User != ent.Owner || args.Weapon != ent.Owner || !args.IsHit)
            return;

        if (HasComp<HumanoidProfileComponent>(ent))
            return;

        foreach (var target in args.HitEntities)
        {
            foreach (var disease in ent.Comp.Diseases.ToList())
            {
                if (!_proto.TryIndex(disease.Prototype, out var proto) || !proto.IsVirus)
                    continue;

                if ((disease.Spread & ADTDiseaseSpread.Bites) == 0)
                    continue;

                Contract(target, disease, ADTDiseaseSpread.Bites | ADTDiseaseSpread.Contact, protectionCheck: true);
            }
        }
    }
}
