using Content.Server.Administration.Logs;
using Content.Server.AlertLevel;
using Content.Server.Station.Systems;
using Content.Server.Kitchen.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.RoundEnd;
using Content.Server.Examine;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Lightning;
using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Singularity.Components;
using Content.Server.Singularity.EntitySystems;
using Content.Server.Traits.Assorted;
using Content.Server.ADT.Hallucinations;
using Content.Shared.ADT.CCVar;
using Content.Shared.ADT.Supermatter.Components;
using Content.Shared.Atmos;
using Content.Shared.Audio;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.DeviceLinking;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.ADT.Supermatter;
using Content.Shared.Radiation.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Supermatter.Systems;

public sealed partial class SupermatterSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly AlertLevelSystem _alert = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly ExamineSystem _examine = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly GravityWellSystem _gravityWell = default!;
    [Dependency] private readonly LightningSystem _lightning = default!;
    [Dependency] private readonly ParacusiaSystem _paracusia = default!;
    [Dependency] private readonly PointLightSystem _light = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambient = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDeviceLinkSystem _link = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly HallucinationsSystem _hallucinations = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SupermatterComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SupermatterComponent, AtmosDeviceUpdateEvent>(OnSupermatterUpdated);

        SubscribeLocalEvent<SupermatterComponent, StartCollideEvent>(OnCollideEvent);
        SubscribeLocalEvent<SupermatterComponent, InteractHandEvent>(OnHandInteract);
        SubscribeLocalEvent<SupermatterComponent, InteractUsingEvent>(OnItemInteract);
        SubscribeLocalEvent<SupermatterComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SupermatterComponent, SupermatterTamperDoAfterEvent>(OnGetSliver);
        SubscribeLocalEvent<SupermatterComponent, SupermatterCoreDoAfterEvent>(OnInsertCore);
        SubscribeLocalEvent<SupermatterComponent, GravPulseEvent>(OnGravPulse);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _zapAccumulator += TimeSpan.FromSeconds(frameTime);
        var shouldZap = false;

        if (_zapAccumulator.TotalSeconds >= 60)
        {
            _zapAccumulator -= TimeSpan.FromSeconds(60);
            shouldZap = true;
        }

        var query = EntityQueryEnumerator<SupermatterComponent>();
        while (query.MoveNext(out var uid, out var sm))
        {
            AnnounceCoreDamage(uid, sm);

            if (shouldZap && EntityManager.EntityExists(uid))
            {
                SupermatterZap(uid, sm, frameTime);
            }
        }
    }

    private void OnMapInit(EntityUid uid, SupermatterComponent sm, MapInitEvent args)
    {
        // Set the yell timer
        sm.YellTimer = TimeSpan.FromSeconds(_config.GetCVar(ADTCCVars.SupermatterYellTimer));

        // Set the sound
        _ambient.SetAmbience(uid, true);

        // Add air to the initialized SM in the map so it doesn't delam on its own
        var mix = _atmosphere.GetContainingMixture(uid, true, true);
        mix?.AdjustMoles(Gas.Oxygen, Atmospherics.OxygenMolesStandard - mix.GetMoles(Gas.Oxygen));
        mix?.AdjustMoles(Gas.Nitrogen, Atmospherics.NitrogenMolesStandard - mix.GetMoles(Gas.Nitrogen));

        // Send the inactive port for any linked devices
        if (HasComp<DeviceLinkSourceComponent>(uid))
            _link.InvokePort(uid, sm.PortInactive);
    }

    public void OnSupermatterUpdated(EntityUid uid, SupermatterComponent sm, AtmosDeviceUpdateEvent args)
    {
        ProcessAtmos(uid, sm, args.dt);
        HandleDamage(uid, sm);
        SupermatterZap(uid, sm, args.dt);

        if (sm.Damage >= sm.DamageDelaminationPoint || sm.Delamming)
            HandleDelamination(uid, sm);

        HandleLight(uid, sm);
        HandleVision(uid, sm);
        HandleStatus(uid, sm);
        HandleSoundLoop(uid, sm);
        HandleAccent(uid, sm);

        if (sm.Power > _config.GetCVar(ADTCCVars.SupermatterPowerPenaltyThreshold) || sm.Damage > sm.DamagePenaltyPoint)
        {
            SupermatterZap(uid, sm, args.dt);
            GenerateAnomalies(uid, sm);
        }
    }

    private void OnCollideEvent(EntityUid uid, SupermatterComponent sm, ref StartCollideEvent args)
    {
        TryCollision(uid, sm, args.OtherEntity, args.OtherBody);
    }

    private void OnHandInteract(EntityUid uid, SupermatterComponent sm, ref InteractHandEvent args)
    {
        var target = args.User;

        if (HasComp<SupermatterImmuneComponent>(target) || HasComp<GodmodeComponent>(target))
            return;

        if (!sm.HasBeenPowered && !HasComp<SupermatterIgnoreComponent>(target))
            LogFirstPower(uid, sm, target);

        var power = 200f;
        if (TryComp<PhysicsComponent>(target, out var physics))
            power += physics.Mass;

        sm.MatterPower += power;

        _popup.PopupEntity(Loc.GetString("supermatter-collide-mob", ("sm", uid), ("target", target)), uid, PopupType.LargeCaution);
        _audio.PlayPvs(sm.DustSound, uid);

        // Prevent spam or excess power production
        AddComp<SupermatterImmuneComponent>(target);

        _chatManager.SendAdminAlert($"{EntityManager.ToPrettyString(uid):uid} has consumed {EntityManager.ToPrettyString(target):target}");
        _adminLog.Add(LogType.EntityDelete, LogImpact.High, $"{EntityManager.ToPrettyString(target):target} touched {EntityManager.ToPrettyString(uid):uid} and was destroyed at {Transform(uid).Coordinates:coordinates}");
        EntityManager.SpawnEntity(sm.CollisionResultPrototype, Transform(target).Coordinates);
        EntityManager.QueueDeleteEntity(target);

        args.Handled = true;
    }

    private void OnItemInteract(EntityUid uid, SupermatterComponent sm, ref InteractUsingEvent args)
    {
        // Ability to cut Sliver Supermatter if the object in your hand is sharp
        if (sm.SliverRemoved)
            return;

        if (HasComp<SharpComponent>(args.Used))
        {
            var doAfterArgs = new DoAfterArgs(EntityManager, args.User, 30f, new SupermatterTamperDoAfterEvent(), args.Target)
            {
                BreakOnDamage = true,
                BreakOnHandChange = false,
                BreakOnWeightlessMove = false,
                NeedHand = true,
                RequireCanInteract = true,
                Used = args.Used,
            };
            _doAfter.TryStartDoAfter(doAfterArgs);
            _popup.PopupClient(Loc.GetString("supermatter-tamper-begin"), uid, args.User);
        }

        if (HasComp<SupermatterNobliumCoreComponent>(args.Used))
        {
            var doAfterArgs = new DoAfterArgs(EntityManager, args.User, 15f, new SupermatterCoreDoAfterEvent(), args.Target)
            {
                BreakOnDamage = true,
                BreakOnHandChange = false,
                BreakOnWeightlessMove = false,
                NeedHand = true,
                RequireCanInteract = true,
                Used = args.Used,
            };
            _doAfter.TryStartDoAfter(doAfterArgs);
            _popup.PopupClient(Loc.GetString("supermatter-inert-begin"), uid, args.User);
        }

        var target = args.User;
        var item = args.Used;
        var othersFilter = Filter.Pvs(uid).RemovePlayerByAttachedEntity(target);

        if (args.Handled ||
        HasComp<GhostComponent>(target) ||
        HasComp<SupermatterImmuneComponent>(item) ||
        HasComp<GodmodeComponent>(item))
            return;

        if (!sm.HasBeenPowered)
            sm.HasBeenPowered = true;

        if (HasComp<UnremoveableComponent>(item))
        {
            if (HasComp<SupermatterIgnoreComponent>(target) || HasComp<SupermatterIgnoreComponent>(item))
                return;

            if (!sm.HasBeenPowered)
                LogFirstPower(uid, sm, target);

            var power = 200f;

            if (TryComp<PhysicsComponent>(target, out var targetPhysics))
                power += targetPhysics.Mass;

            if (TryComp<PhysicsComponent>(item, out var itemPhysics))
                power += itemPhysics.Mass;

            sm.MatterPower += power;

            _popup.PopupEntity(Loc.GetString("supermatter-collide-insert-unremoveable", ("target", target), ("sm", uid), ("item", item)), uid, othersFilter, true, PopupType.LargeCaution);
            _popup.PopupEntity(Loc.GetString("supermatter-collide-insert-unremoveable-user", ("sm", uid), ("item", item)), uid, target, PopupType.LargeCaution);
            _audio.PlayPvs(sm.DustSound, uid);

            // Prevent spam or excess power production
            AddComp<SupermatterImmuneComponent>(target);
            AddComp<SupermatterImmuneComponent>(item);

            _adminLog.Add(LogType.EntityDelete, LogImpact.High, $"{EntityManager.ToPrettyString(target):target} touched {EntityManager.ToPrettyString(uid):uid} with {EntityManager.ToPrettyString(item):item} and was destroyed at {Transform(uid).Coordinates:coordinates}");
            EntityManager.SpawnEntity(sm.CollisionResultPrototype, Transform(target).Coordinates);
            EntityManager.QueueDeleteEntity(target);
            EntityManager.QueueDeleteEntity(item);
        }
        else
        {
            if (TryComp<PhysicsComponent>(item, out var physics))
                sm.MatterPower += physics.Mass;

            _popup.PopupEntity(Loc.GetString("supermatter-collide-insert", ("target", target), ("sm", uid), ("item", item)), uid, othersFilter, true, PopupType.LargeCaution);
            _popup.PopupEntity(Loc.GetString("supermatter-collide-insert-user", ("sm", uid), ("item", item)), uid, target, PopupType.LargeCaution);
            _audio.PlayPvs(sm.DustSound, uid);

            // Prevent spam or excess power production
            AddComp<SupermatterImmuneComponent>(item);

            _adminLog.Add(LogType.EntityDelete, LogImpact.High, $"{EntityManager.ToPrettyString(target):target} touched {EntityManager.ToPrettyString(uid):uid} with {EntityManager.ToPrettyString(item):item} and destroyed it at {Transform(uid).Coordinates:coordinates}");
            EntityManager.QueueDeleteEntity(item);

            if (HasComp<SupermatterIgnoreComponent>(target) || HasComp<SupermatterIgnoreComponent>(item))
                return;

            if (!sm.HasBeenPowered)
                LogFirstPower(uid, sm, item);
        }

        args.Handled = true;
    }

    private void OnGetSliver(EntityUid uid, SupermatterComponent sm, ref SupermatterTamperDoAfterEvent args)
    {
        if (args.Cancelled || !args.Used.HasValue)
            return;

        QueueDel(args.Used.Value);

        sm.Damage += sm.DamageDelaminationPoint / 10;

        var message = Loc.GetString("supermatter-announcement-cc-tamper");
        SendSupermatterAnnouncement(uid, sm, message, global: false);

        Spawn(sm.SliverPrototype, _transform.GetMapCoordinates(args.User));
        _popup.PopupClient(Loc.GetString("supermatter-tamper-end"), uid, args.User);
    }

    private void OnInsertCore(EntityUid uid, SupermatterComponent sm, ref SupermatterCoreDoAfterEvent args)
    {
        if (args.Cancelled || !args.Used.HasValue)
            return;

        QueueDel(args.Used.Value);

        var message = Loc.GetString("supermatter-announcement-setinert");
        SendSupermatterAnnouncement(uid, sm, message, global: false);

        sm.HasBeenPowered = false;

        if (TryComp<RadiationSourceComponent>(uid, out var rad))
            rad.Intensity = 1;

        _popup.PopupClient(Loc.GetString("supermatter-inert-end"), uid, args.User);
    }

    private void OnGravPulse(Entity<SupermatterComponent> ent, ref GravPulseEvent args)
    {
        if (!TryComp<GravityWellComponent>(ent, out var gravityWell))
            return;

        var nextPulse = 0.5f * _random.NextFloat(1f, 30f);
        _gravityWell.SetPulsePeriod(ent, TimeSpan.FromSeconds(nextPulse), gravityWell);

        var audioParams = AudioParams.Default.WithMaxDistance(gravityWell.MaxRange);
        _audio.PlayPvs(ent.Comp.PullSound, ent, audioParams);
    }

    private void OnExamine(EntityUid uid, SupermatterComponent sm, ref ExaminedEvent args)
    {
        // For ghosts: alive players can use the console
        if (HasComp<GhostComponent>(args.Examiner) && args.IsInDetailsRange)
            args.PushMarkup(Loc.GetString("supermatter-examine-integrity", ("integrity", GetIntegrity(sm).ToString("0.00"))));
    }

    private void TryCollision(EntityUid uid, SupermatterComponent sm, EntityUid target, PhysicsComponent? targetPhysics = null, bool checkStatic = true)
    {
        if (!Resolve(target, ref targetPhysics))
            return;

        if (targetPhysics.BodyType == BodyType.Static && checkStatic ||
            HasComp<SupermatterImmuneComponent>(target) ||
            HasComp<GodmodeComponent>(target) ||
            _container.IsEntityInContainer(uid))
            return;

        if (!HasComp<ProjectileComponent>(target))
        {
            if (HasComp<MobStateComponent>(target))
            {
                EntityManager.SpawnEntity(sm.CollisionResultPrototype, Transform(target).Coordinates);
            }

            var targetProto = MetaData(target).EntityPrototype;
            if (targetProto != null && targetProto.ID != sm.CollisionResultPrototype)
            {
                _popup.PopupEntity(Loc.GetString("supermatter-collide-mob", ("sm", uid), ("target", target)), uid, PopupType.LargeCaution);
                _audio.PlayPvs(sm.DustSound, uid);
            }

            sm.MatterPower += targetPhysics.Mass;
            _adminLog.Add(LogType.EntityDelete, LogImpact.High, $"{EntityManager.ToPrettyString(target):target} collided with {EntityManager.ToPrettyString(uid):uid} at {Transform(uid).Coordinates:coordinates}");
        }

        // Prevent spam or excess power production
        AddComp<SupermatterImmuneComponent>(target);

        EntityManager.QueueDeleteEntity(target);

        if (TryComp<SupermatterFoodComponent>(target, out var food))
            sm.Power += food.Energy;
        else if (TryComp<ProjectileComponent>(target, out var projectile))
            sm.Power += (float)projectile.Damage.GetTotal();
        else
            sm.Power++;

        sm.MatterPower += HasComp<MobStateComponent>(target) ? 200 : 0;

        if (!HasComp<SupermatterIgnoreComponent>(target) && !sm.HasBeenPowered)
            LogFirstPower(uid, sm, target);
    }

    private void LogFirstPower(EntityUid uid, SupermatterComponent sm, EntityUid target)
    {
        _adminLog.Add(LogType.Unknown, LogImpact.Extreme, $"{EntityManager.ToPrettyString(uid):uid} was powered for the first time by {EntityManager.ToPrettyString(target):target} at {Transform(uid).Coordinates:coordinates}");
        _chatManager.SendAdminAlert($"{EntityManager.ToPrettyString(uid):uid} was powered for the first time by {EntityManager.ToPrettyString(target):target}");
        sm.HasBeenPowered = true;
    }

    private void LogFirstPower(EntityUid uid, SupermatterComponent sm, GasMixture gas)
    {
        _adminLog.Add(LogType.Unknown, LogImpact.Extreme, $"{EntityManager.ToPrettyString(uid):uid} was powered for the first time by gas mixture at {Transform(uid).Coordinates:coordinates}");
        _chatManager.SendAdminAlert($"{EntityManager.ToPrettyString(uid):uid} was powered for the first time by gas mixture");
        sm.HasBeenPowered = true;
    }

    private SupermatterStatusType GetStatus(EntityUid uid, SupermatterComponent sm)
    {
        var mix = _atmosphere.GetContainingMixture(uid, true, true);

        if (mix is not { })
            return SupermatterStatusType.Error;

        if (sm.Delamming || sm.Damage >= sm.DamageDelaminationPoint)
            return SupermatterStatusType.Delaminating;

        if (sm.Damage >= sm.DamagePenaltyPoint)
            return SupermatterStatusType.Emergency;

        if (sm.Damage >= sm.DamageDelamAlertPoint)
            return SupermatterStatusType.Danger;

        if (sm.Damage >= sm.DamageWarningThreshold)
            return SupermatterStatusType.Warning;

        if (mix.Temperature > Atmospherics.T0C + _config.GetCVar(ADTCCVars.SupermatterHeatPenaltyThreshold) * 0.8)
            return SupermatterStatusType.Caution;

        if (sm.Power > 5)
            return SupermatterStatusType.Normal;

        return SupermatterStatusType.Inactive;
    }
}