using Content.Shared.Atmos.Rotting;
using Content.Shared.ADT.Atmos.Rotting;
using Robust.Shared.GameObjects;

namespace Content.Server.ADT.Atmos.Rotting;

public sealed partial class UnRottingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnRottingComponent, IsRottingEvent>(OnIsRotting);
    }

    private void OnIsRotting(EntityUid uid, UnRottingComponent component, ref IsRottingEvent args)
    {
        args.Handled = true;
    }
}
