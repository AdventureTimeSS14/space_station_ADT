using Content.Shared.ADT.Areas;
using Content.Shared.ADT.Xenobiology.XenobiologyControlConsole;

namespace Content.Shared.ADT.Xenobiology;

public sealed class SharedXenobiologyEyeSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenobiologyEyeComponent, MoveEvent>(OnEyeMove);
    }

    private void OnEyeMove(Entity<XenobiologyEyeComponent> ent, ref MoveEvent args)
    {
        if (ent.Comp.IsProcessingMoveEvent || TerminatingOrDeleted(ent))
            return;

        if (_area.GetAreaPrototypeId(args.NewPosition) == ent.Comp.AllowedArea)
            return;

        ent.Comp.IsProcessingMoveEvent = true;
        _xform.SetCoordinates(ent, args.OldPosition);
        ent.Comp.IsProcessingMoveEvent = false;
    }
}
