using System.Linq;
using Content.Server.ADT.Legion;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.ADT.Language;
using Content.Shared.ADT.Rituals;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Pinpointer;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Weather;
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
    public bool ToBeacon;

    public override void Effect(IEntityManager entMan, ADTRitualArgs args)
    {
        var transform = entMan.System<SharedTransformSystem>();
        var targets = entMan.System<ADTRitualSystem>().GetTargets(args, Target);

        if (!ToBeacon)
        {
            var runeCoords = entMan.GetComponent<TransformComponent>(args.Object).Coordinates;

            foreach (var target in targets)
            {
                transform.SetCoordinates(target, runeCoords);
            }

            return;
        }

        var beacons = FindBeacons(entMan);

        if (beacons.Count == 0)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();

        foreach (var target in targets)
        {
            var beacon = random.Pick(beacons);
            transform.SetCoordinates(target, entMan.GetComponent<TransformComponent>(beacon).Coordinates);
        }
    }

    private static List<EntityUid> FindBeacons(IEntityManager entMan)
    {
        var found = new List<EntityUid>();
        var query = entMan.EntityQueryEnumerator<NavMapBeaconComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (xform.MapUid != null)
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

    [DataField]
    public ProtoId<LanguagePrototype> SpokenLanguage = "Draconic";

    [DataField]
    public ProtoId<LanguagePrototype> CollectiveMindLanguage = "ADTAshWalkerCollectiveMind";

    public override void Effect(IEntityManager entMan, ADTRitualArgs args)
    {
        var damageable = entMan.System<DamageableSystem>();
        var mobState = entMan.System<MobStateSystem>();
        var mobThreshold = entMan.System<MobThresholdSystem>();
        var factions = entMan.System<NpcFactionSystem>();
        var language = entMan.System<SharedLanguageSystem>();
        var blood = entMan.System<SharedBloodstreamSystem>();

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

            var speaker = entMan.EnsureComponent<LanguageSpeakerComponent>(target);
            language.AddSpokenLanguage(target, SpokenLanguage, LanguageKnowledge.Speak, speaker);
            language.AddSpokenLanguage(target, CollectiveMindLanguage, LanguageKnowledge.Understand, speaker);

            if (entMan.TryGetComponent<BloodstreamComponent>(target, out var bloodstream))
            {
                blood.TryRegulateBloodLevel((target, bloodstream), bloodstream.BloodReferenceSolution.Volume, 1f);

                if (bloodstream.BleedAmount > 0)
                    blood.TryModifyBleedAmount((target, bloodstream), -bloodstream.BleedAmount);
            }

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

public sealed partial class ADTRitualLegionInfestEffect : ADTRitualEffect
{
    [DataField]
    public ADTRitualTarget Target = ADTRitualTarget.RandomTribesman;

    [DataField]
    public EntProtoId Legion = "ADTMobLegion";

    [DataField]
    public LocId Message = "adt-legion-infest";

    public override void Effect(IEntityManager entMan, ADTRitualArgs args)
    {
        var legion = entMan.System<ADTLegionSkullSystem>();

        foreach (var target in entMan.System<ADTRitualSystem>().GetTargets(args, Target))
        {
            legion.InfestTarget(target, Legion, Message, args.Object);
        }
    }
}
