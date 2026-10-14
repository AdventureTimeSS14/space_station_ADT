using Content.Shared.Actions;
using Content.Shared.ADT.AnomalousCrystal;
using Content.Shared.Administration.Systems;
using Content.Shared.Damage.Systems;
using Content.Shared.Gibbing;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.AnomalousCrystal;

public sealed partial class ADTAnomalousCrystalSystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private SharedGodmodeSystem _godmode = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    private void InitializePossession()
    {
        SubscribeLocalEvent<ADTCrystalPossessedComponent, ADTExitPossessionActionEvent>(OnExitPossession);
        SubscribeLocalEvent<ADTCrystalPossessedComponent, MobStateChangedEvent>(OnPossessedStateChanged);
    }

    public bool TryPossess(EntityUid user, EntityUid mindId, EntityUid animal, EntProtoId exitAction)
    {
        var possessed = EnsureComp<ADTCrystalPossessedComponent>(animal);
        var stasis = _container.EnsureContainer<ContainerSlot>(animal, possessed.ContainerId);

        if (!_container.Insert(user, stasis))
        {
            RemComp<ADTCrystalPossessedComponent>(animal);
            return false;
        }

        possessed.Body = user;
        _godmode.EnableGodmode(user);
        _mind.TransferTo(mindId, animal);
        possessed.Action = _actions.AddAction(animal, exitAction);
        return true;
    }

    private void OnExitPossession(Entity<ADTCrystalPossessedComponent> ent, ref ADTExitPossessionActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ReleaseBody(ent, false);
        _gibbing.Gib(ent);
    }

    private void OnPossessedStateChanged(Entity<ADTCrystalPossessedComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        ReleaseBody(ent, true);
        _gibbing.Gib(ent);
    }

    private void ReleaseBody(Entity<ADTCrystalPossessedComponent> ent, bool kill)
    {
        var body = ent.Comp.Body;

        _actions.RemoveAction(ent.Comp.Action);

        if (TerminatingOrDeleted(body))
            return;

        if (_container.TryGetContainer(ent, ent.Comp.ContainerId, out var stasis))
            _container.Remove(body, stasis, destination: Transform(ent).Coordinates);

        _godmode.DisableGodmode(body);

        if (_mind.TryGetMind(ent, out var mindId, out _))
            _mind.TransferTo(mindId, body);

        if (kill)
            _mobState.ChangeMobState(body, MobState.Dead);

        RemCompDeferred<ADTCrystalPossessedComponent>(ent);
    }
}
