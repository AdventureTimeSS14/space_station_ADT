using Content.Shared.ADT.Xenobiology.XenobiologyControlConsole;

namespace Content.Shared.ADT.Xenobiology;

public sealed class SharedXenobiologyEyeSystem : EntitySystem
{
    // TODO: система зон вырезана, вернуть зависимости при переделке OnEyeMove
    // [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;

    public override void Initialize()
    {
        base.Initialize();

        // TODO: ждем математику
        // SubscribeLocalEvent<XenobiologyEyeComponent, MoveEvent>(OnEyeMove);
    }

    // TODO: ждем математику
    // private void OnEyeMove(Entity<XenobiologyEyeComponent> ent, ref MoveEvent args)
    // {
    //     var areaProto = _area.GetAreaPrototypeId(args.NewPosition);
    //
    //     if (areaProto == null || areaProto != "ADTAreaXenobio")
    //     {
    //         _xform.SetCoordinates(ent.Owner, args.OldPosition);
    //     }
    // }
}