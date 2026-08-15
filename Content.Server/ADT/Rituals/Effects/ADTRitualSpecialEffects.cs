using System.Linq;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.ADT.Rituals;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Weather;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Rituals.Effects;

public sealed partial class ADTRitualPolymorphEffect : ADTRitualEffect
{
    [DataField]
    public ADTRitualTarget Target = ADTRitualTarget.Invoker;

    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Polymorph = default!;

    [DataField]
    public bool RequireMind = true;

    public override void Effect(IEntityManager entMan, ADTRitualArgs args)
    {
        var polymorph = entMan.System<PolymorphSystem>();

        foreach (var target in entMan.System<ADTRitualSystem>().GetTargets(args, Target))
        {
            if (RequireMind
                && (!entMan.TryGetComponent<MindContainerComponent>(target, out var mind) || !mind.HasMind))
                continue;

            polymorph.PolymorphEntity(target, Polymorph);
        }
    }
}

public sealed partial class ADTRitualTeleportEffect : ADTRitualEffect
{
    [DataField]
    public ADTRitualTarget Target = ADTRitualTarget.UsedThings;

    [DataField]
    public EntityWhitelist? Destination;

    public override void Effect(IEntityManager entMan, ADTRitualArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        var transform = entMan.System<SharedTransformSystem>();
        var runeCoords = entMan.GetComponent<TransformComponent>(args.Object).Coordinates;
        var beacons = Destination == null ? null : FindBeacons(entMan, Destination);

        foreach (var target in entMan.System<ADTRitualSystem>().GetTargets(args, Target))
        {
            if (beacons == null)
            {
                transform.SetCoordinates(target, runeCoords);
                continue;
            }

            if (beacons.Count == 0)
                continue;

            var beacon = random.Pick(beacons);
            transform.SetCoordinates(target, entMan.GetComponent<TransformComponent>(beacon).Coordinates);
        }
    }

    private static List<EntityUid> FindBeacons(IEntityManager entMan, EntityWhitelist whitelist)
    {
        var whitelistSystem = entMan.System<EntityWhitelistSystem>();
        var found = new List<EntityUid>();
        var query = entMan.EntityQueryEnumerator<TransformComponent>();

        while (query.MoveNext(out var uid, out var xform))
        {
            if (xform.MapUid != null && whitelistSystem.IsValid(whitelist, uid))
                found.Add(uid);
        }

        return found;
    }
}

public sealed partial class ADTRitualReviveEffect : ADTRitualEffect
{
    [DataField]
    public ADTRitualTarget Target = ADTRitualTarget.UsedThings;

    [DataField]
    public DamageSpecifier? Aftermath;

    public override void Effect(IEntityManager entMan, ADTRitualArgs args)
    {
        var damageable = entMan.System<DamageableSystem>();
        var mobState = entMan.System<MobStateSystem>();
        var mobThreshold = entMan.System<MobThresholdSystem>();

        foreach (var target in entMan.System<ADTRitualSystem>().GetTargets(args, Target))
        {
            if (!entMan.HasComponent<MobStateComponent>(target))
                continue;

            damageable.SetAllDamage(target, 0);
            mobThreshold.SetAllowRevives(target, true);
            mobState.ChangeMobState(target, MobState.Alive);
            mobThreshold.SetAllowRevives(target, false);

            if (Aftermath != null)
                damageable.TryChangeDamage(target, Aftermath, true);
        }
    }
}

public sealed partial class ADTRitualWeatherEffect : ADTRitualEffect
{
    [DataField(required: true)]
    public EntProtoId Weather = default!;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromMinutes(3);

    public override void Effect(IEntityManager entMan, ADTRitualArgs args)
    {
        var weather = entMan.System<SharedWeatherSystem>();
        var map = entMan.GetComponent<TransformComponent>(args.Object).MapID;

        weather.TryAddWeather(map, Weather, out _, Duration);
    }
}

public sealed partial class ADTRitualRechargeEffect : ADTRitualEffect
{
    [DataField]
    public int Amount = 1;

    [DataField]
    public List<ProtoId<ADTRitualPrototype>> Blacklist = new();

    public override void Effect(IEntityManager entMan, ADTRitualArgs args)
    {
        var rituals = entMan.System<ADTRitualSystem>();

        if (!entMan.TryGetComponent<ADTRitualObjectComponent>(args.Object, out var obj))
            return;

        foreach (var ritual in rituals.GetRitualsOf((args.Object, obj)))
        {
            if (Blacklist.Contains(ritual.ID))
                continue;

            rituals.AddCharge((args.Object, obj), ritual, Amount);
        }
    }
}

public sealed partial class ADTRitualEmpathEffect : ADTRitualEffect
{
    [DataField]
    public ADTRitualTarget Target = ADTRitualTarget.UsedThings;

    public override void Effect(IEntityManager entMan, ADTRitualArgs args)
    {
        var popup = entMan.System<SharedPopupSystem>();
        var mobState = entMan.System<MobStateSystem>();

        foreach (var target in entMan.System<ADTRitualSystem>().GetTargets(args, Target))
        {
            if (!entMan.HasComponent<MobStateComponent>(target))
                continue;

            var damage = entMan.TryGetComponent<DamageableComponent>(target, out var damageable)
                ? (int)damageable.TotalDamage
                : 0;

            var feeling = mobState.IsDead(target)
                ? "adt-ritual-empath-dead"
                : mobState.IsCritical(target)
                    ? "adt-ritual-empath-dying"
                    : damage > 50
                        ? "adt-ritual-empath-hurt"
                        : "adt-ritual-empath-well";

            popup.PopupEntity(
                Loc.GetString("adt-ritual-empath-report",
                    ("target", target),
                    ("feeling", Loc.GetString(feeling))),
                args.Invoker,
                args.Invoker,
                PopupType.Medium);
        }
    }
}

public sealed partial class ADTRitualSentienceEffect : ADTRitualEffect
{
    [DataField]
    public ADTRitualTarget Target = ADTRitualTarget.UsedThings;

    [DataField]
    public LocId RoleName = "ghost-role-information-adt-ash-walker-slave-name";

    [DataField]
    public LocId RoleDescription = "ghost-role-information-adt-ash-walker-slave-description";

    [DataField]
    public ProtoId<NpcFactionPrototype> Faction = "ADTAshWalker";

    public override void Effect(IEntityManager entMan, ADTRitualArgs args)
    {
        var damageable = entMan.System<DamageableSystem>();
        var mobState = entMan.System<MobStateSystem>();
        var mobThreshold = entMan.System<MobThresholdSystem>();
        var factions = entMan.System<NpcFactionSystem>();

        foreach (var target in entMan.System<ADTRitualSystem>().GetTargets(args, Target))
        {
            if (!entMan.HasComponent<MobStateComponent>(target))
                continue;

            if (entMan.TryGetComponent<MindContainerComponent>(target, out var mind) && mind.HasMind)
                continue;

            damageable.SetAllDamage(target, 0);
            mobThreshold.SetAllowRevives(target, true);
            mobState.ChangeMobState(target, MobState.Alive);
            mobThreshold.SetAllowRevives(target, false);

            factions.ClearFactions(target);
            factions.AddFaction(target, Faction);

            if (entMan.HasComponent<GhostRoleComponent>(target))
                continue;

            var role = entMan.AddComponent<GhostRoleComponent>(target);
            entMan.EnsureComponent<GhostTakeoverAvailableComponent>(target);
            role.RoleName = Loc.GetString(RoleName);
            role.RoleDescription = Loc.GetString(RoleDescription);
        }
    }
}

public sealed partial class ADTRitualSummonPickerEffect : ADTRitualEffect
{
    [DataField]
    public ADTRitualTarget Candidates = ADTRitualTarget.Tribe;

    public override void Effect(IEntityManager entMan, ADTRitualArgs args)
    {
        var candidates = entMan.System<ADTRitualSystem>().GetTargets(args, Candidates);

        candidates.RemoveAll(args.Invokers.Contains);

        if (candidates.Count == 0)
            return;

        entMan.System<ADTRitualSummonSystem>().OpenPicker(args.Object, args.Invoker, candidates);
    }
}
