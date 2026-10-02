using Content.Shared.Actions;
using Content.Shared.ADT.Fishing.Components;

namespace Content.Shared.ADT.Fishing.Systems;

public sealed class ADTFishingRodActionSystem : EntitySystem
{
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTFishingRodComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTFishingRodComponent, GetItemActionsEvent>(OnGetActions);
    }

    private void OnMapInit(Entity<ADTFishingRodComponent> ent, ref MapInitEvent args)
    {
        _actionContainer.EnsureAction(ent.Owner, ref ent.Comp.CastActionEntity, ent.Comp.CastAction);
        Dirty(ent);
    }

    private void OnGetActions(Entity<ADTFishingRodComponent> ent, ref GetItemActionsEvent args)
    {
        if (!args.InHands)
            return;

        args.AddAction(ref ent.Comp.CastActionEntity, ent.Comp.CastAction);
    }
}
