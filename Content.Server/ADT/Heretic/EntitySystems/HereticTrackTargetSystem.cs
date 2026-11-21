using Content.Shared.ADT.Heretic.Common;
using Robust.Shared.Utility;

namespace Content.Server.ADT.Heretic.EntitySystems;

public sealed partial class HereticTrackTargetSystem : EntitySystem
{
    public static readonly SpriteSpecifier BeamSprite =
        new SpriteSpecifier.Rsi(new("ADT/Heretic/Effects/effects.rsi"), "cosmic_beam");

    public void ShowBeam(EntityUid performer, EntityUid target)
    {
        var performerNet = GetNetEntity(performer);

        ClearBeam(performerNet);

        var beam = EnsureComp<ComplexJointVisualsComponent>(target);
        beam.Data[performerNet] = new ComplexJointVisualsData(
            "heretic-track",
            BeamSprite,
            Color.FromHex("#22e07a"));
        Dirty(target, beam);
    }

    private void ClearBeam(NetEntity performer)
    {
        var stale = new List<EntityUid>();
        var query = AllEntityQuery<ComplexJointVisualsComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            if (!comp.Data.ContainsKey(performer))
                continue;

            comp.Data.Remove(performer);
            Dirty(ent, comp);

            if (comp.Data.Count == 0)
                stale.Add(ent);
        }

        foreach (var ent in stale)
            RemComp<ComplexJointVisualsComponent>(ent);
    }
}
