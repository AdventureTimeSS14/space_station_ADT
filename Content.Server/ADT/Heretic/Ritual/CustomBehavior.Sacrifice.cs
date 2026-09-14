//

using System.Linq;
using System.Numerics;
using Content.Shared.ADT.Heretic;
using Content.Server.ADT.Objectives.Components;
using Content.Server.Heretic.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Managers;
using Content.Server.Heretic.EntitySystems;
using Content.Server.Medical.SuitSensors;
using Content.Server.Objectives.Components;
using Content.Goobstation.Shared.Changeling.Components;
using Content.Shared.Chat;
using Content.Shared.Damage.Systems;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Mobs.Components;
using Robust.Shared.Prototypes;
using Content.Shared.Humanoid;
using Content.Server.Revolutionary.Components;
using Content.Shared.Mind;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Store.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Mobs;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Heretic.Ritual;

/// <summary>
///     Checks for a nearest dead body,
///     teleports it away and gives the heretic knowledge points.
/// </summary>
/// <remarks>
///     ADT: no gib, teleport away instead + coord sensors
/// </remarks>
// these classes should be lead out and shot
[Virtual] public partial class RitualSacrificeBehavior : RitualCustomBehavior
{
    /// <summary>
    ///     Minimal amount of corpses.
    /// </summary>
    [DataField]
    public float Min = 1;

    /// <summary>
    ///     Maximum amount of corpses.
    /// </summary>
    [DataField]
    public float Max = 1;

    /// <summary>
    ///     Should we count only targets?
    /// </summary>
    [DataField]
    public bool OnlyTargets;

    /// <summary>
    ///     Should we count only humanoids?
    /// </summary>
    [DataField]
    public bool OnlyHumanoid = true;

    // this is awful but it works so i'm not complaining
    protected SharedMindSystem _mind = default!;
    protected HereticSystem _heretic = default!;
    protected EntityLookupSystem _lookup = default!;
    [Dependency] protected IPrototypeManager _proto = default!;
    [Dependency] protected ILogManager _log = default!;

    private ISawmill? _sawmill;

    protected List<EntityUid> uids = new();

    public override bool Execute(RitualData args, out string? outstr)
    {
        _mind = args.EntityManager.System<SharedMindSystem>();
        _heretic = args.EntityManager.System<HereticSystem>();
        _lookup = args.EntityManager.System<EntityLookupSystem>();
        _proto = IoCManager.Resolve<IPrototypeManager>();
        _log = IoCManager.Resolve<ILogManager>();

        uids = new();

        var hereticComp = args.Mind.Comp;

        var lookup = _lookup.GetEntitiesInRange(args.Platform, 1.5f);
        if (lookup.Count == 0)
        {
            outstr = Loc.GetString("heretic-ritual-fail-sacrifice");
            return false;
        }

        // get all the dead ones
        foreach (var look in lookup)
        {
            // ADT: never count the performer, they may be standing on the rune
            if (look == args.Performer)
                continue;

            if (!args.EntityManager.TryGetComponent<MobStateComponent>(look, out var mobstate) // only mobs
            || OnlyHumanoid && !args.EntityManager.HasComponent<HumanoidProfileComponent>(look) // only humans
            || args.EntityManager.HasComponent<BorgChassisComponent>(look) // no borgs
            || OnlyTargets
                && hereticComp.SacrificeTargets.All(x => x.Entity != args.EntityManager.GetNetEntity(look)) // only targets
                && !_heretic.TryGetHereticComponent(look, out _, out _)) // or other heretics
                continue;

            if (mobstate.CurrentState != Shared.Mobs.MobState.Alive)
                uids.Add(look);
        }

        if (uids.Count < Min)
        {
            outstr = Loc.GetString("heretic-ritual-fail-sacrifice-ineligible");
            return false;
        }

        outstr = null;
        return true;
    }

    public override void Finalize(RitualData args)
    {
        var heretic = args.Mind.Comp;

        if (!args.EntityManager.TryGetComponent(args.Mind, out StoreComponent? store) ||
            !args.EntityManager.TryGetComponent(args.Mind, out MindComponent? mind))
            return;

        var knowledgeGain = 0f;
        for (var i = 0; i < Max && i < uids.Count; i++)
        {
            if (!args.EntityManager.EntityExists(uids[i]))
                continue;

            var uid = uids[i];

            var isCommand = args.EntityManager.HasComponent<CommandStaffComponent>(uid);
            var isSec = args.EntityManager.HasComponent<SecurityStaffComponent>(uid);
            var isHeretic = _heretic.TryGetHereticComponent(uid, out var otherHeretic, out var otherMind);

            if (isHeretic)
                knowledgeGain += 4f;
            else if (heretic.SacrificeTargets.Any(x => x.Entity == args.EntityManager.GetNetEntity(uid)))
                knowledgeGain += isCommand || isSec ? 3f : 2f;

            try
            {
                // ADT: no gib, teleport + coord sensors instead
                SafeSacrifice(args, uid);
            }
            catch (Exception e)
            {
                _sawmill ??= _log.GetSawmill("sacrifice");
                _sawmill.Error(e.Message);
            }

            // Sacrificed heretics lose their powers forever
            if (otherMind != EntityUid.Invalid && otherHeretic is { } h)
                args.EntityManager.RemoveComponentDeferred(otherMind, h);

            // update objectives
            // this is godawful dogshit. but it works :)
            if (_mind.TryFindObjective((args.Mind, mind), "HereticSacrificeObjective", out var crewObj)
            && args.EntityManager.TryGetComponent<HereticSacrificeConditionComponent>(crewObj, out var crewObjComp))
                crewObjComp.Sacrificed += 1;

            if (_mind.TryFindObjective((args.Mind, mind), "HereticSacrificeHeadObjective", out var crewHeadObj)
            && args.EntityManager.TryGetComponent<HereticSacrificeConditionComponent>(crewHeadObj, out var crewHeadObjComp)
            && isCommand)
                crewHeadObjComp.Sacrificed += 1;
        }

        if (knowledgeGain > 0)
            _heretic.UpdateMindKnowledge((args.Mind, args.Mind.Comp, store, mind), args.Performer, knowledgeGain);

        // reset it because it refuses to work otherwise.
        uids = new();
        args.EntityManager.EventBus.RaiseLocalEvent(args.Mind, new EventHereticUpdateTargets());
    }

    /// <summary>
    ///     ADT: teleports the corpse away instead of gibbing it.
    /// </summary>
    private void SafeSacrifice(RitualData args, EntityUid uid)
    {
        var entMan = args.EntityManager;
        var suitSensorSystem = entMan.System<SuitSensorSystem>();
        var sharedMindSystem = entMan.System<SharedMindSystem>();
        var chatManager = IoCManager.Resolve<IChatManager>();
        var player = IoCManager.Resolve<ISharedPlayerManager>();

        if (!entMan.TryGetComponent<MobStateComponent>(uid, out var mobstate))
            return;

        if (mobstate.CurrentState == MobState.Dead)
        {
            TeleportRandomly(args, uid);

            // notify the victim
            var message = Loc.GetString("sacrificed-description");
            var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));

            if (sharedMindSystem.TryGetMind(uid, out _, out _) &&
                player.TryGetSessionByEntity(uid, out var session))
            {
                chatManager.ChatMessageToOne(ChatChannel.Server,
                    message,
                    wrappedMessage,
                    default,
                    false,
                    session.Channel,
                    Color.FromSrgb(new Color(255, 100, 255)));
            }

            entMan.EnsureComponent<SacrificedComponent>(uid);
        }

        // coord sensors so the body can be found
        suitSensorSystem.SetAllSensors(uid, SuitSensorMode.SensorCords);
    }

    /// <summary>
    ///     ADT: picks a random tile with atmosphere and no walls.
    /// </summary>
    private void TeleportRandomly(RitualData args, EntityUid uid)
    {
        var entMan = args.EntityManager;
        var xformSystem = entMan.System<SharedTransformSystem>();
        var lookupSystem = entMan.System<EntityLookupSystem>();
        var atmosSystem = entMan.System<AtmosphereSystem>();
        var pullSystem = entMan.System<PullingSystem>();
        var randomSystem = IoCManager.Resolve<IRobustRandom>();

        const int maxRandomTp = 50; // attempts to find a spot
        const int maxRandomRadius = 40; // max scatter radius

        if (!entMan.TryGetComponent<TransformComponent>(uid, out var transformComponent))
            return;

        // stop pull or the heretic gets dragged along
        if (entMan.TryGetComponent<PullableComponent>(uid, out var pull))
            pullSystem.TryStopPull(uid, pull);

        var coords = transformComponent.Coordinates;

        for (var i = 0; i < maxRandomTp; i++)
        {
            var randVector = randomSystem.NextVector2(maxRandomRadius);
            var newCoords = coords.Offset(randVector);

            // move first so we can check for walls
            xformSystem.SetCoordinates(uid, newCoords);

            // no atmosphere = space/solar, keep looking
            var air = atmosSystem.GetContainingMixture((uid, transformComponent));

            if (transformComponent.GridUid != null
                && air != null
                && !lookupSystem.GetEntitiesIntersecting(xformSystem.ToMapCoordinates(newCoords), LookupFlags.Static).Any())
                break;
        }
    }
}
