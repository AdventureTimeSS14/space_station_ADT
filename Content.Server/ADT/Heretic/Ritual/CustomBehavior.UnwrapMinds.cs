using Content.Shared.ADT.Heretic.Components.Side;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Implants.Components;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Paper;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
namespace Content.Server.ADT.Heretic.Ritual;
public sealed partial class RitualUnwrapMindsBehavior : RitualCustomBehavior
{
    private EntityUid _target;
    private string _order = string.Empty;
    public override bool Execute(RitualData args, out string? outstr)
    {
        _target = EntityUid.Invalid;
        _order = string.Empty;
        var entMan = args.EntityManager;
        var lookup = entMan.System<EntityLookupSystem>();
        var tagSys = entMan.System<TagSystem>();
        EntityUid? paper = null;
        foreach (var ent in lookup.GetEntitiesInRange(args.Platform, 1.5f))
        {
            if (ent == args.Performer)
                continue;
            if (entMan.TryGetComponent<PaperComponent>(ent, out var paperComp))
            {
                paper = ent;
                _order = paperComp.Content.Trim();
            }
        }
        if (paper == null)
        {
            outstr = Loc.GetString("heretic-ritual-fail-unwrap-no-paper");
            return false;
        }
        if (string.IsNullOrWhiteSpace(_order))
        {
            outstr = Loc.GetString("heretic-ritual-fail-unwrap-blank");
            return false;
        }
        var foundTool = false;
        foreach (var ent in lookup.GetEntitiesInRange(args.Platform, 1.5f))
        {
            if (ent == args.Performer)
                continue;
            if (tagSys.HasTag(ent, "SurgeryTool"))
                foundTool = true;
        }
        if (!foundTool)
        {
            outstr = Loc.GetString("heretic-ritual-fail-unwrap-no-scalpel");
            return false;
        }
        var mindSys = entMan.System<SharedMindSystem>();
        foreach (var ent in lookup.GetEntitiesInRange(args.Platform, 1.5f))
        {
            if (ent == args.Performer)
                continue;
            if (!entMan.HasComponent<HumanoidProfileComponent>(ent))
                continue;
            if (!entMan.TryGetComponent<MobStateComponent>(ent, out var state))
                continue;
            if (state.CurrentState != MobState.Alive)
                continue;
            if (!mindSys.TryGetMind(ent, out _, out _))
                continue;
            if (entMan.HasComponent<HereticHypnosisComponent>(ent))
                continue;
            _target = ent;
            break;
        }
        if (_target == EntityUid.Invalid)
        {
            outstr = Loc.GetString("heretic-ritual-fail-unwrap-no-victim");
            return false;
        }
        outstr = null;
        return true;
    }
    public override void Finalize(RitualData args)
    {
        var entMan = args.EntityManager;
        if (_target == EntityUid.Invalid || !entMan.EntityExists(_target))
            return;
        var hyp = entMan.EnsureComponent<HereticHypnosisComponent>(_target);
        hyp.Order = _order.Length > 500 ? _order.Substring(0, 500) : _order;
        entMan.Dirty(_target, hyp);
        if (entMan.TryGetComponent<ImplantedComponent>(_target, out var implanted))
        {
            var toDel = new List<EntityUid>();
            foreach (var imp in implanted.ImplantContainer.ContainedEntities)
            {
                if (entMan.HasComponent<MindShieldImplantComponent>(imp))
                    toDel.Add(imp);
            }
            foreach (var del in toDel)
                entMan.QueueDeleteEntity(del);
        }
        if (entMan.HasComponent<MindShieldComponent>(_target))
            entMan.RemoveComponent<MindShieldComponent>(_target);
    }
}
