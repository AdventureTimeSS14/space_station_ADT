using Content.Shared.Actions;
using Content.Shared.ADT.Phantom.Components;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.ADT.Phantom;

public abstract class SharedPhantomSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// Select phantom style (abilities pack)
    /// </summary>
    /// <param name="uid">Phantom uid</param>
    /// <param name="component">Phantom component</param>
    /// <param name="style">Style protoId</param>
    /// <param name="force">Force or not</param>
    public void SelectStyle(EntityUid uid, PhantomComponent component, string style, bool force = false)
    {
        if (!_proto.TryIndex<PhantomStylePrototype>(style, out var proto))
            return;
        if (style == component.CurrentStyle && !force)
            return;
        foreach (var action in component.CurrentActions)
        {
            _action.RemoveAction(uid, action);
            if (action != null)
                component.TempContainedActions.Add(action.Value);
        }
        component.CurrentActions.Clear();

        if (component.IgnoreLevels)
        {
            AddFromList(uid, component, proto.Lvl1Actions);
            AddFromList(uid, component, proto.Lvl2Actions);
            AddFromList(uid, component, proto.Lvl3Actions);
            AddFromList(uid, component, proto.Lvl4Actions);
            AddFromList(uid, component, proto.Lvl5Actions);
            component.CurrentStyle = style;
            return;
        }

        var level = component.Vessels.Count;
        var curseds = component.CursedVessels.Count;
        if (level <= 0)
            return;
        if (level > 0)
            AddFromList(uid, component, proto.Lvl1Actions);
        if (level > 2)
            AddFromList(uid, component, proto.Lvl2Actions);
        if (level > 4)
            AddFromList(uid, component, proto.Lvl3Actions);
        if (level > 6 && curseds > 0)
            AddFromList(uid, component, proto.Lvl4Actions);
        if (level > 9 && curseds > 1)
            AddFromList(uid, component, proto.Lvl5Actions);
        component.CurrentStyle = style;

        if (component.MaxReachedLevel < level)
        {
            var ev = new PhantomLevelReachedEvent(level);
            component.MaxReachedLevel = level;
            RaiseLocalEvent(uid, ref ev);
        }
    }

    /// <summary>
    /// Adding all actions from list
    /// </summary>
    /// <param name="uid">Phantom uid</param>
    /// <param name="component">Phantom component</param>
    /// <param name="list">Actions protoId list</param>
    public void AddFromList(EntityUid uid, PhantomComponent component, List<string> list)
    {
        foreach (var action in list)
        {
            if (action == null)
                continue;
            var actionEntity = new EntityUid?();
            _action.AddAction(uid, ref actionEntity, action);
            component.CurrentActions.Add(actionEntity);

            foreach (var item in component.TempContainedActions.ToList())
            {
                if (TryPrototype(item, out var proto) &&
                    proto.ID == action)
                {
                    if (_action.TryGetActionData(item, out var data) && data.Cooldown != null)
                    {
                        _action.SetCooldown(actionEntity, data.Cooldown.Value.Start, data.Cooldown.Value.End);
                        component.TempContainedActions.Remove(item);
                        QueueDel(item);
                    }
                }
            }
            if (actionEntity != null)
                DirtyEntity(actionEntity.Value);
            DirtyEntity(uid);
        }
    }
}
