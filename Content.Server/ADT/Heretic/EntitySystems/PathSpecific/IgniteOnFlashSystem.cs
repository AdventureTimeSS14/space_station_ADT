//

using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos.Components;
using Content.Shared.Flash;
using Content.Shared.Heretic.Components.PathSpecific.Ash;

namespace Content.Server.ADT.Heretic.EntitySystems.PathSpecific;

public sealed partial class IgniteOnFlashSystem : EntitySystem
{
    [Dependency] private readonly FlammableSystem _flammable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IgniteOnFlashComponent, AfterFlashedEvent>(OnFlashed);
    }

    private void OnFlashed(Entity<IgniteOnFlashComponent> ent, ref AfterFlashedEvent args)
    {
        if (args.Used != ent.Owner)
            return;

        var target = args.Target;
        if (!TryComp(target, out FlammableComponent? flam))
            return;

        _flammable.AdjustFireStacks(target, ent.Comp.FireStacks, flam);
        if (ent.Comp.FireStacks > 0f)
            _flammable.Ignite(target, ent, flam, args.User);
    }
}
