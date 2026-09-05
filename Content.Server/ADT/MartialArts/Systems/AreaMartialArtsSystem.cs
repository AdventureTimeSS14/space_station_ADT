using Content.Shared.ADT.Areas;
using Content.Shared.ADT.MartialArts;
using Content.Server.ADT.MartialArts.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.MartialArts.Systems;

public sealed class AreaMartialArtsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedMartialArtsSystem _martialArts = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<AreaMartialArtComponent, MoveEvent>(OnMove);
    }

    private void OnPlayerAttach(PlayerAttachedEvent args)
    {
        if (!TryComp<AreaMartialArtComponent>(args.Entity, out var comp))
            return;

        comp.LastArea = _area.GetAreaPrototypeId(Transform(args.Entity).Coordinates);
        ApplyGrants(args.Entity, comp);
    }

    private void OnMove(Entity<AreaMartialArtComponent> ent, ref MoveEvent args)
    {
        var area = _area.GetAreaPrototypeId(args.NewPosition);
        if (area == ent.Comp.LastArea)
            return;

        ent.Comp.LastArea = area;
        ApplyGrants(ent.Owner, ent.Comp);
    }

    private void ApplyGrants(EntityUid owner, AreaMartialArtComponent comp)
    {
        if (TerminatingOrDeleted(owner))
            return;

        var inArea = _area.GetAreaPrototypeId(Transform(owner).Coordinates)?.Id == comp.Area.Id;
        var form = _proto.Index(comp.MartialArt).MartialArtsForm;

        if (!TryComp<MartialArtsKnowledgeComponent>(owner, out var knowledge))
        {
            if (inArea && _martialArts.TryGrantMartialArt(owner, comp.MartialArt, comp.LearnMessage))
                EnsureComp<AreaMartialArtGrantedComponent>(owner);
            return;
        }

        if (knowledge.MartialArtsForm != form)
            return;

        if (!HasComp<AreaMartialArtGrantedComponent>(owner))
        {
            if (knowledge.Blocked)
            {
                knowledge.Blocked = false;
                Dirty(owner, knowledge);
            }
            return;
        }

        if (inArea == knowledge.Blocked)
        {
            knowledge.Blocked = !inArea;
            Dirty(owner, knowledge);
        }
    }
}