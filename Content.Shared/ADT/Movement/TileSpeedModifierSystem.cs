using Content.Shared.Maps;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Map;

namespace Content.Shared.ADT.Movement;
public sealed class TileSpeedModifierSystem : EntitySystem
{
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MovementSpeedModifierComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<TileSpeedModifierComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    private void OnMove(Entity<MovementSpeedModifierComponent> ent, ref MoveEvent args)
    {
        if (args.NewPosition == args.OldPosition)
            return;

        var speed = GetTileSpeedModifier(args.NewPosition);
        if (MathHelper.CloseTo(speed, 1f))
        {
            if (HasComp<TileSpeedModifierComponent>(ent))
            {
                RemComp<TileSpeedModifierComponent>(ent);
                _speedModifier.RefreshMovementSpeedModifiers(ent);
            }

            return;
        }

        var comp = EnsureComp<TileSpeedModifierComponent>(ent);
        if (MathHelper.CloseTo(comp.WalkSpeedModifier, speed)
            && MathHelper.CloseTo(comp.SprintSpeedModifier, speed))
        {
            return;
        }

        comp.WalkSpeedModifier = speed;
        comp.SprintSpeedModifier = speed;
        _speedModifier.RefreshMovementSpeedModifiers(ent);
    }

    private void OnRefreshSpeed(Entity<TileSpeedModifierComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        args.ModifySpeed(ent.Comp.WalkSpeedModifier, ent.Comp.SprintSpeedModifier);
    }

    private float GetTileSpeedModifier(EntityCoordinates coordinates)
    {
        if (!_turf.TryGetTileRef(coordinates, out var tileRef))
            return 1f;

        return _turf.GetContentTileDefinition(tileRef.Value).SpeedModifier;
    }
}
