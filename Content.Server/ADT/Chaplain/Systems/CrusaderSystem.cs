using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.ADT.Chaplain.Systems;

namespace Content.Server.ADT.Chaplain.Systems;

public sealed partial class CrusaderSystem : EntitySystem
{
    [Dependency] private SharedChaplainSystem _chaplain = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrusaderComponent, MapInitEvent>(OnMapInit);

        InitializeCrusade();
    }

    private void OnMapInit(Entity<CrusaderComponent> ent, ref MapInitEvent args)
    {
        var map = Transform(ent).MapID;

        if (ent.Comp.Master)
        {
            if (!TryComp<ChaplainComponent>(ent, out var chaplain))
                return;

            var query = EntityQueryEnumerator<CrusaderComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out var crusader, out var xform))
            {
                if (crusader.Master || xform.MapID != map)
                    continue;

                if (TryComp<ChaplainFollowerComponent>(uid, out var follower) && follower.Chaplain != null)
                    continue;

                _chaplain.LinkFollower((ent, chaplain), uid);
            }

            return;
        }

        var masters = EntityQueryEnumerator<CrusaderComponent, ChaplainComponent, TransformComponent>();
        while (masters.MoveNext(out var uid, out var crusader, out var chaplain, out var xform))
        {
            if (!crusader.Master || xform.MapID != map)
                continue;

            _chaplain.LinkFollower((uid, chaplain), ent);
            return;
        }
    }
}
