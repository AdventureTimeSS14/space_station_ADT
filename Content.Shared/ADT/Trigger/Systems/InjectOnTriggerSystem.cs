using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Trigger;
using Robust.Shared.Containers;

namespace Content.Shared.ADT.Trigger;

public sealed class InjectOnTriggerSystem : XOnTriggerSystem<InjectOnTriggerComponent>
{
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly StomachSystem _stomach = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InjectOnTriggerComponent, AttemptTriggerEvent>(OnAttempt);
    }

    private void OnAttempt(Entity<InjectOnTriggerComponent> ent, ref AttemptTriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;
        if (target is not { } uid || HasSpace(uid, ent.Comp))
            return;

        args.Cancelled = true;

        if (ent.Comp.FullPopup is { } popup && args.User is { } user)
            _popup.PopupClient(Loc.GetString(popup), user, user, PopupType.SmallCaution);
    }

    protected override void OnTrigger(Entity<InjectOnTriggerComponent> ent, EntityUid target, ref TriggerEvent args)
    {
        var injected = ent.Comp.Destination switch
        {
            InjectDestination.Bloodstream => _bloodstream.TryAddToBloodstream(target, ent.Comp.Solution),
            InjectDestination.Stomach => GetStomach(target) is { } stomach && _stomach.TryTransferSolution(stomach, ent.Comp.Solution),
            _ => false,
        };

        if (injected)
            args.Handled = true;
    }

    private bool HasSpace(EntityUid target, InjectOnTriggerComponent comp)
    {
        switch (comp.Destination)
        {
            case InjectDestination.Bloodstream:
                return TryComp<BloodstreamComponent>(target, out var bloodstream)
                    && _solutionContainer.ResolveSolution(target, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var blood)
                    && blood.AvailableVolume >= comp.Solution.Volume;
            case InjectDestination.Stomach:
                return GetStomach(target) is { } stomach && _stomach.CanTransferSolution(stomach, comp.Solution);
            default:
                return false;
        }
    }

    private EntityUid? GetStomach(EntityUid body)
    {
        if (!_container.TryGetContainer(body, BodyComponent.ContainerID, out var organs))
            return null;

        foreach (var organ in organs.ContainedEntities)
        {
            if (HasComp<StomachComponent>(organ))
                return organ;
        }

        return null;
    }
}
