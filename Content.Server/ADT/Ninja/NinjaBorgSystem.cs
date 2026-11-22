using Content.Server.Objectives.Components;
using Content.Shared.ADT.Ninja.Components;
using Content.Shared.ADT.Silicons.Borgs.Components;
using Content.Shared.Administration.Logs;
using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.ADT.Ninja;

public sealed class NinjaBorgSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedNinjaGlovesSystem _gloves = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAccessSystem _access = default!;
    [Dependency] private readonly SharedBorgSwitchableTypeSystem _switchable = default!;
    [Dependency] private readonly SharedBorgSystem _borgSystem = default!;
    [Dependency] private readonly NinjaADTConditionsSystem _conditions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NinjaBorgConvertComponent, BeforeInteractHandEvent>(OnTryConvert);
    }

    private void OnTryConvert(Entity<NinjaBorgConvertComponent> ent, ref BeforeInteractHandEvent args)
    {
        if (args.Handled || !_gloves.AbilityCheck(ent, args, out var target))
            return;

        if (HasComp<NinjaBorgChassisComponent>(target))
            return;

        if (HasComp<AiRemoteControllerComponent>(target))
        {
            args.Handled = true;
            _popup.PopupEntity(Loc.GetString("ninja-borg-convert-ai-popup"), target, ent, PopupType.MediumCaution);
            return;
        }

        if (!TryComp<BorgChassisComponent>(target, out var chassis))
            return;

        if (_mobState.IsCritical(target) || _mobState.IsDead(target))
        {
            var prototype = PickNinjaChassis(ent, target);
            if (prototype == null)
            {
                Log.Error($"{ToPrettyString(ent)}: NinjaBorgConvertComponent has no ChassisByBorgType/FallbackChassis configured - conversion aborted");
                return;
            }

            var newUid = ConvertNinjaBorg((target, chassis), prototype.Value);
            if (newUid is not { } borg)
                return;

            args.Handled = true;

            if (_mind.TryGetObjectiveComp<BorgHackConditionComponent>(ent, out var condition))
                _conditions.TryRegisterHackedBorg(condition, borg);

            _audio.PlayPvs(new SoundCollectionSpecifier("sparks"), borg);

            _popup.PopupEntity(
                Loc.GetString("ninja-borg-convert-popup", ("target", borg)),
                borg,
                ent,
                PopupType.Large);

            _adminLogger.Add(
                LogType.Emag,
                LogImpact.High,
                $"{ToPrettyString(ent):player} clicked a vulnerable borg {ToPrettyString(target):target} and revived it as a ninja borg {ToPrettyString(borg):borg}");
            return;
        }

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("ninja-borg-convert-alive-popup"), target, ent, PopupType.MediumCaution);
    }

    private EntProtoId? PickNinjaChassis(Entity<NinjaBorgConvertComponent> ent, EntityUid oldChassis)
    {
        if (_switchable.GetSelectedBorgType((oldChassis, (BorgSwitchableTypeComponent?) null)) is { } borgType)
        {
            if (ent.Comp.ChassisByBorgType.TryGetValue(borgType, out var mapped))
                return mapped;

            Log.Warning($"{ToPrettyString(oldChassis)}: no chassis mapped for borg type '{borgType}' in NinjaBorgConvertComponent - using fallback");
        }

        var fallback = ent.Comp.FallbackChassis;
        if (fallback.Count == 0)
            return null;

        return _random.Pick(fallback);
    }

    public EntityUid? ConvertNinjaBorg(Entity<BorgChassisComponent> oldBorg, EntProtoId prototype)
    {
        if (TerminatingOrDeleted(oldBorg.Owner))
            return null;

        var oldXform = Transform(oldBorg);

        var newBorg = Spawn(prototype, oldXform.Coordinates);
        if (!TryComp<BorgChassisComponent>(newBorg, out var newChassis))
        {
            Del(newBorg);
            return null;
        }

        _transform.SetLocalRotation(newBorg, oldXform.LocalRotation);

        var oldTags = _access.TryGetTags(oldBorg);
        if (oldTags != null)
        {
            var newTags = new HashSet<ProtoId<AccessLevelPrototype>>(oldTags);
            if (_access.TryGetTags(newBorg) is { } baseTags)
                newTags.UnionWith(baseTags);
            _access.TrySetTags(newBorg, newTags);
        }

        if (oldBorg.Comp.BrainContainer.ContainedEntity is { Valid: true } brain && !TerminatingOrDeleted(brain))
        {
            _container.Remove(brain, oldBorg.Comp.BrainContainer, force: true);

            if (newChassis.BrainContainer.ContainedEntity is { Valid: true } autoBrain)
                Del(autoBrain);

            _container.Insert(brain, newChassis.BrainContainer);
        }

        foreach (var module in new List<EntityUid>(oldBorg.Comp.ModuleContainer.ContainedEntities))
        {
            var modulePrototype = MetaData(module).EntityPrototype;
            if (modulePrototype == null)
            {
                Log.Error($"{ToPrettyString(module)}: borg module without prototype is lost on ninja conversion");
                continue;
            }

            var copy = Spawn(modulePrototype.ID, oldXform.Coordinates);
            _borgSystem.InsertModule((newBorg, newChassis), copy);
        }

        QueueDel(oldBorg);

        return newBorg;
    }
}
