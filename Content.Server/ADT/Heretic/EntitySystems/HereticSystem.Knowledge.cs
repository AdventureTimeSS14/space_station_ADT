//

using Content.Shared.Heretic.Prototypes;
using Content.Shared.Heretic;
using Content.Shared.Mind;
using Robust.Shared.Prototypes;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class HereticSystem
{
    public HereticKnowledgePrototype GetKnowledge(ProtoId<HereticKnowledgePrototype> id)
        => _proto.Index(id);

    /// <summary>
    ///     ADT: root knowledge, locks in the heretic's path.
    /// </summary>
    public static bool IsRootKnowledge(HereticKnowledgePrototype data)
        => !data.SideKnowledge && data.Stage <= 1 && !string.IsNullOrWhiteSpace(data.Path);

    public void RaiseKnowledgeEvent(EntityUid uid, HereticKnowledgeEvent ev, bool negative)
    {
        if (negative)
            EntityManager.RemoveComponents(uid, ev.AddedComponents);
        else
            EntityManager.AddComponents(uid, ev.AddedComponents);
        ev.Negative = negative;
        ev.Heretic = uid;
        RaiseLocalEvent(uid, (object) ev, true);
    }

    public bool TryAddKnowledge(Entity<HereticComponent?> ent,
        ProtoId<HereticKnowledgePrototype> id,
        EntityUid? body = null)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        body ??= CompOrNull<MindComponent>(ent.Owner)?.CurrentEntity;

        var data = GetKnowledge(id);

        // ADT: hard path lock once CurrentPath is set
        if (!data.SideKnowledge
            && !string.IsNullOrWhiteSpace(data.Path)
            && !string.IsNullOrWhiteSpace(ent.Comp.CurrentPath)
            && ent.Comp.CurrentPath != data.Path)
            return false;

        if (data.Event != null && body != null)
        {
            RaiseKnowledgeEvent(body.Value, data.Event, false);
            ent.Comp.KnowledgeEvents.Add(data.Event);
        }

        if (data.ActionPrototypes is { Count: > 0 })
        {
            foreach (var act in data.ActionPrototypes)
            {
                _actionContainer.AddAction(ent.Owner, act);
            }
        }

        if (data.RitualPrototypes is { Count: > 0 })
        {
            foreach (var ritual in data.RitualPrototypes)
            {
                ent.Comp.KnownRituals.Add(_ritual.GetRitual(ritual));
            }
        }

        // set path if our heretic doesn't have it yet
        if (string.IsNullOrWhiteSpace(ent.Comp.CurrentPath) && !data.SideKnowledge)
            ent.Comp.CurrentPath = data.Path;

        // make sure we only progress when buying current path knowledge
        if (data.Stage > ent.Comp.PathStage && data.Path == ent.Comp.CurrentPath)
            ent.Comp.PathStage = data.Stage;

        // ADT: Dirty after CurrentPath/PathStage write, not before
        Dirty(ent);

        return true;
    }
}
