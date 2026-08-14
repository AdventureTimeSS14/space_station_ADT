using Content.Shared.ADT.AshWalker.Components;
using Content.Shared.ADT.Lavaland;
using Content.Shared.Movement.Systems;

namespace Content.Shared.ADT.AshWalker.Systems;

public sealed class ADTLavalandSpeedSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTLavalandSpeedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<ADTLavalandSpeedComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTLavalandSpeedComponent, EntParentChangedMessage>(OnParentChanged);
    }

    private void OnRefreshSpeed(Entity<ADTLavalandSpeedComponent> ent, RefreshMovementSpeedModifiersEvent args)
    {
        if (!IsBonusActive(ent))
            return;

        args.ModifySpeed(ent.Comp.WalkModifier, ent.Comp.SprintModifier);
    }

    private void OnMapInit(Entity<ADTLavalandSpeedComponent> ent, ref MapInitEvent args)
    {
        _movement.RefreshMovementSpeedModifiers(ent);
    }

    private void OnParentChanged(Entity<ADTLavalandSpeedComponent> ent, ref EntParentChangedMessage args)
    {
        if (args.OldMapId == Transform(ent).MapUid)
            return;

        _movement.RefreshMovementSpeedModifiers(ent);
    }

    private bool IsBonusActive(Entity<ADTLavalandSpeedComponent> ent)
    {
        if (ent.Comp.Everywhere)
            return true;

        if (Transform(ent).MapUid is not { } map)
            return false;

        return HasComp<ADTLavalandMapComponent>(map);
    }
}
