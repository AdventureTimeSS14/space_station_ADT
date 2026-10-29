using Content.Shared.Interaction.Events;
using Content.Shared.Revolutionary.Components;
using Content.Shared.UserInterface;

namespace Content.Shared.ADT.Revolutinary.Stash;

public sealed class ADTRevolutionStashSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTRevolutionStashComponent, GettingInteractedWithAttemptEvent>(OnInteractAttempt);
        SubscribeLocalEvent<ADTRevolutionStashComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
    }

    private void OnInteractAttempt(Entity<ADTRevolutionStashComponent> ent, ref GettingInteractedWithAttemptEvent args)
    {
        if (!CanUse(args.Uid))
            args.Cancelled = true;
    }

    private void OnUIOpenAttempt(Entity<ADTRevolutionStashComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (!CanUse(args.User))
            args.Cancel();
    }

    public bool CanUse(EntityUid user)
    {
        return HasComp<HeadRevolutionaryComponent>(user);
    }
}
