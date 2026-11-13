using Content.Shared.ADT.Cooking;
using Content.Shared.ADT.Cooking.Components;
using Content.Shared.ADT.Cooking.Prototypes;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;

namespace Content.Server.ADT.Cooking;

public sealed partial class ADTCookingSystem
{
    private readonly HashSet<Entity<MobStateComponent>> _fireTargets = new();

    private bool CheckFlare(Entity<ADTCookwareComponent> ent, ADTStoveHeat heat, ADTCookingMediumPrototype? medium, ADTCookingSettingsPrototype settings)
    {
        if (ent.Comp.Type != ADTCookwareType.Pan || medium == null || !medium.Flammable)
            return false;

        if (heat < settings.FlareMinHeat)
            return false;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.Solution, out _, out var solution))
            return false;

        if (GetWater(solution, settings) < settings.FlareMinWater)
            return false;

        StartFire(ent, settings);
        return true;
    }

    private void StartFire(Entity<ADTCookwareComponent> ent, ADTCookingSettingsPrototype settings)
    {
        var comp = ent.Comp;
        BurnContents(ent, settings);

        if (_solution.TryGetSolution(ent.Owner, comp.Solution, out var solnEnt, out _))
            _solution.RemoveAllSolution(solnEnt.Value);

        comp.FireEndTime = _timing.CurTime + settings.FireDuration;
        comp.TargetKey = null;
        ResetMinigames(comp);
        SetStage(ent, ADTCookingStage.OnFire);

        var popupTarget = ent.Owner;
        if (TryGetStoveSlot(ent, out var stove, out var slot))
        {
            popupTarget = stove;
            stove.Comp.Slots[slot].Heat = ADTStoveHeat.Off;
            Dirty(stove);
        }

        _audio.PlayPvs(settings.FlareSound, popupTarget);
        _popup.PopupEntity(Loc.GetString("adt-cookware-flare", ("cookware", ent.Owner)), popupTarget, PopupType.LargeCaution);

        _fireTargets.Clear();
        _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(ent.Owner), settings.FireRange, _fireTargets);
        foreach (var mob in _fireTargets)
        {
            _flammable.AdjustFireStacks(mob, settings.FireStacks, ignite: true);
        }

        UpdateAppearance(ent);
    }

    private void EndFire(Entity<ADTCookwareComponent> ent)
    {
        ent.Comp.FireEndTime = null;
        SetStage(ent, ent.Comp.Contents.ContainedEntities.Count > 0 ? ADTCookingStage.Burnt : ADTCookingStage.Idle);

        if (!TryGetStoveSlot(ent, out var stove, out var slot) || stove.Comp.Slots[slot].Heat == ADTStoveHeat.Off)
            StopActive(ent);
        else
            UpdateAppearance(ent);
    }
}
