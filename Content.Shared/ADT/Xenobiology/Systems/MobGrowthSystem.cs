using Content.Shared.ADT.Xenobiology.Components;
using Content.Shared.ADT.Xenobiology;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Xenobiology.Systems;

public sealed partial class MobGrowthSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobGrowthComponent, ComponentInit>(OnMobGrowthInit);
    }

    private void OnMobGrowthInit(Entity<MobGrowthComponent> ent, ref ComponentInit args)
    {
        ent.Comp.NextGrowthTime = _gameTiming.CurTime + ent.Comp.GrowthInterval;
        ent.Comp.BaseEntityName = Name(ent);

        if (!ent.Comp.Stages.ContainsKey(ent.Comp.CurrentStage))
        {
            Log.Error($"Invalid initial stage {ent.Comp.CurrentStage} for entity {ToPrettyString(ent)}");
            ent.Comp.CurrentStage = ent.Comp.FirstStage;
        }

        UpdateAppearance(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MobGrowthComponent, HungerComponent>();
        while (query.MoveNext(out var uid, out var growth, out var hungerComp))
        {
            if (_gameTiming.CurTime < growth.NextGrowthTime)
                continue;

            growth.NextGrowthTime = _gameTiming.CurTime + growth.GrowthInterval;

            if (_mobState.IsDead(uid)
                || _hunger.GetHunger(hungerComp) < growth.HungerRequired
                || !growth.Stages.TryGetValue(growth.CurrentStage, out var currentData)
                || string.IsNullOrEmpty(currentData.NextStage))
                continue;

            DoGrowth((uid, growth, hungerComp));
        }
    }

    private void DoGrowth(Entity<MobGrowthComponent, HungerComponent> ent)
    {
        var (uid, growth, hunger) = ent;

        if (TerminatingOrDeleted(ent))
            return;

        if (!growth.Stages.TryGetValue(growth.CurrentStage, out var currentStageData))
        {
            Log.Error($"Missing stage data for {growth.CurrentStage} on entity {ToPrettyString(uid)}");
            return;
        }

        if (currentStageData.NextStage is not { } nextStage || !growth.Stages.ContainsKey(nextStage))
        {
            Log.Error($"Invalid next stage {currentStageData.NextStage} for entity {ToPrettyString(uid)}");
            return;
        }

        _hunger.ModifyHunger(uid, growth.GrowthCost, hunger);
        growth.CurrentStage = nextStage;
        Dirty(uid, growth);

        UpdateAppearance((uid, growth));
    }

    private void UpdateAppearance(Entity<MobGrowthComponent> ent)
    {
        if (!ent.Comp.Stages.TryGetValue(ent.Comp.CurrentStage, out var stageData)
            || !TryComp<AppearanceComponent>(ent, out var appearance)
            || stageData.Sprite is not { } sprite)
            return;

        _appearance.SetData(ent, GrowthStateVisuals.Sprite, sprite, appearance);

        if (_net.IsServer)
        {
            var displayName = string.IsNullOrEmpty(stageData.DisplayName)
                ? string.Empty
                : Loc.GetString(stageData.DisplayName);

            _metaData.SetEntityName(ent, $"{displayName} {ent.Comp.BaseEntityName}");
        }
    }

    public void SetBaseName(EntityUid uid, string baseName)
    {
        if (!TryComp<MobGrowthComponent>(uid, out var growth))
            return;

        growth.BaseEntityName = baseName;
        UpdateAppearance((uid, growth));
    }
}
