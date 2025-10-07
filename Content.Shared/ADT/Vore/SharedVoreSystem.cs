using System.Collections.Generic;
using Content.Shared.Actions;
using Content.Shared.ADT.Vore.Components;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Vore;

public abstract class SharedVoreSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem AudioSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] protected readonly SharedContainerSystem ContainerSystem = default!;
    [Dependency] protected readonly SharedTransformSystem TransformSystem = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VoreComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<VoreComponent, VoreActionEvent>(OnVoreAction);
        SubscribeLocalEvent<VoreComponent, VoreReleaseActionEvent>(OnReleaseAction);
        SubscribeLocalEvent<VoreComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(EntityUid uid, VoreComponent component, MapInitEvent args)
    {
        component.Stomach = ContainerSystem.EnsureContainer<Container>(uid, "vore_stomach");

        _actionsSystem.AddAction(uid, ref component.VoreActionEntity, component.VoreAction);
        _actionsSystem.AddAction(uid, ref component.ReleaseActionEntity, component.ReleaseAction);

        UpdateReleaseActionState(uid, component);
    }

    private void OnShutdown(EntityUid uid, VoreComponent component, ComponentShutdown args)
    {
        if (component.Stomach != null)
        {
            var entities = new List<EntityUid>(component.Stomach.ContainedEntities);
            foreach (var entity in entities)
            {
                ReleaseEntity(uid, entity, component, silent: true);
            }
        }
    }

    private void OnVoreAction(EntityUid uid, VoreComponent component, VoreActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var target = args.Target;

        if (component.VoredEntities.Count >= component.MaxCapacity)
        {
            if (_netManager.IsClient)
                _popupSystem.PopupClient(Loc.GetString("vore-full"), uid, uid);
            return;
        }

        if (!TryComp(target, out MobStateComponent? targetState))
        {
            if (_netManager.IsClient)
                _popupSystem.PopupClient(Loc.GetString("vore-invalid-target"), uid, uid);
            return;
        }

        _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, uid, component.VoreTime, new VoreDoAfterEvent(), uid, target: target, used: uid)
        {
            BreakOnMove = true,
        });
    }

    private void OnReleaseAction(EntityUid uid, VoreComponent component, VoreReleaseActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (component.VoredEntities.Count == 0)
            return;

        var toRelease = component.VoredEntities[^1];
        ReleaseEntity(uid, toRelease, component);
    }

    protected void ReleaseEntity(EntityUid devourer, EntityUid target, VoreComponent component, bool silent = false)
    {
        if (!component.Stomach.Contains(target))
            return;

        RemComp<VoredComponent>(target);
        ContainerSystem.Remove(target, component.Stomach);
        component.VoredEntities.Remove(target);

        var xform = Transform(devourer);
        TransformSystem.SetCoordinates(target, xform.Coordinates);

        RemComp<VoreOverlayComponent>(target);

        if (!silent)
        {
            AudioSystem.PlayPvs(component.ReleaseSound, devourer);
        }

        UpdateReleaseActionState(devourer, component);
        Dirty(devourer, component);
    }

    protected void UpdateReleaseActionState(EntityUid uid, VoreComponent component)
    {
        if (component.ReleaseActionEntity.HasValue)
        {
            _actionsSystem.SetEnabled(component.ReleaseActionEntity, component.VoredEntities.Count > 0);
        }
    }
}

public sealed partial class VoreActionEvent : EntityTargetActionEvent { }

public sealed partial class VoreReleaseActionEvent : InstantActionEvent { }

[Serializable, NetSerializable]
public sealed partial class VoreDoAfterEvent : SimpleDoAfterEvent { }

