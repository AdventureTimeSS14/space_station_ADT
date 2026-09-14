using Content.Shared.ADT.Areas;

namespace Content.Shared.ADT.EyeControl;

public sealed class SharedEyeControlSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EyeControlEyeComponent, MoveEvent>(OnEyeMove);
    }

    private void OnEyeMove(Entity<EyeControlEyeComponent> ent, ref MoveEvent args)
    {
        if (ent.Comp.IsProcessingMoveEvent || TerminatingOrDeleted(ent))
            return;

        if (ent.Comp.AllowedArea is null)
            return;

        if (_area.GetAreaPrototypeId(args.NewPosition) == ent.Comp.AllowedArea)
            return;

        ent.Comp.IsProcessingMoveEvent = true;
        _xform.SetCoordinates(ent, args.OldPosition);
        ent.Comp.IsProcessingMoveEvent = false;
    }
}
