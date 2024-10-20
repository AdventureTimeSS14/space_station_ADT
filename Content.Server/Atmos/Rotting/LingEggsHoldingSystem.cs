using Content.Shared.Changeling.Components;
using Content.Shared.Examine;
using Content.Shared.Mobs.Systems;

namespace Content.Server.Atmos.Rotting;

public sealed partial class LingEggsHoldingSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<LingEggsHolderComponent, ExaminedEvent>(OnExamine);
        base.Initialize();
    }

    private void OnExamine(EntityUid uid, LingEggsHolderComponent component, ExaminedEvent args)
    {
        if (!_mobState.IsDead(uid))
            return;
        args.PushMarkup(Loc.GetString("adt-rotting-ling-eggs"));
    }
}
