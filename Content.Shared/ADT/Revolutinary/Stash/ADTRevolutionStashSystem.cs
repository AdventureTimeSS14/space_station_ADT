using Content.Shared.Interaction;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Thief;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Shared.ADT.Revolutinary.Stash;

public sealed class ADTRevolutionStashSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedStorageSystem _storage = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTRevolutionStashComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ADTRevolutionStashCompartmentComponent, AccessibleOverrideEvent>(OnCompartmentAccessible);
    }

    private void OnGetVerbs(Entity<ADTRevolutionStashComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !CanUse(args.User))
            return;

        var user = args.User;

        if (TryGetCompartment(ent, out var compartment))
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("adt-revolution-stash-verb"),
                Act = () => _storage.OpenStorageUI(compartment, user, silent: false),
            });
        }

        if (HasComp<ADTRevolutionKitComponent>(ent))
        {
            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("adt-revolution-kit-verb"),
                Act = () => _ui.OpenUi(ent.Owner, ThiefBackpackUIKey.Key, user, true),
            });
        }
    }

    private void OnCompartmentAccessible(Entity<ADTRevolutionStashCompartmentComponent> ent, ref AccessibleOverrideEvent args)
    {
        args.Handled = true;
        args.Accessible = false;

        if (!CanUse(args.User))
            return;

        if (!_container.TryGetContainingContainer(ent.Owner, out var container))
            return;

        args.Accessible = _interaction.IsAccessible(args.User, container.Owner);
    }

    private bool TryGetCompartment(Entity<ADTRevolutionStashComponent> ent, out EntityUid compartment)
    {
        compartment = default;

        if (!_container.TryGetContainer(ent, ent.Comp.ContainerId, out var container))
            return false;

        foreach (var contained in container.ContainedEntities)
        {
            if (!HasComp<ADTRevolutionStashCompartmentComponent>(contained))
                continue;

            compartment = contained;
            return true;
        }

        return false;
    }

    public bool CanUse(EntityUid user)
    {
        return HasComp<HeadRevolutionaryComponent>(user);
    }
}
