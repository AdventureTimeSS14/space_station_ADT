using Content.Shared.ADT.DrunkDrift;
using Content.Shared.Examine;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Shared.ADT.DrunkDrift;

/// <summary>ADT: осмотр пьяных в shared, чтобы текст появлялся без задержки пинга.</summary>
public abstract class SharedDrunkDriftSystem : EntitySystem
{
    private EntityQuery<MobStateComponent> _mobQuery;

    public override void Initialize()
    {
        _mobQuery = GetEntityQuery<MobStateComponent>();

        SubscribeLocalEvent<ADTDrunkDriftComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<ADTDrunkDriftComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.VisualsActive)
            return;

        if (!_mobQuery.TryComp(ent.Owner, out var mob) || mob.CurrentState != MobState.Alive)
            return;

        args.PushMarkup(Loc.GetString("adt-drunk-examine", ("ent", ent.Owner)));
    }
}
