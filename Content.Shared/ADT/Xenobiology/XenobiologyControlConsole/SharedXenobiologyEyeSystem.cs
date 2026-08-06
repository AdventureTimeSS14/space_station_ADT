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
        var areaProto = _area.GetAreaPrototypeId(args.NewPosition);

        if (areaProto == null || areaProto != "ADTAreaXenobio")
        {
            _xform.SetCoordinates(ent.Owner, args.OldPosition);
        }
    }
}