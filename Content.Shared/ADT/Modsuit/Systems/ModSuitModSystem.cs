using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Shared.PowerCell;

namespace Content.Shared.ADT.ModSuits;

public sealed class SharedModSuitModSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ModSuitSystem _mod = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModSuitModComponent, BeforeRangedInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ModSuitModComponent, ModModulesUiStateReadyEvent>(OnGetUIState);

        SubscribeLocalEvent<ModSuitComponent, ModModuleRemoveMessage>(OnEject);
        SubscribeLocalEvent<ModSuitComponent, ModModulActivateMessage>(OnActivate);
        SubscribeLocalEvent<ModSuitComponent, ModModulDeactivateMessage>(OnDeactivate);
    }
    private void OnEject(EntityUid uid, ModSuitComponent component, ModModuleRemoveMessage args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        var module = GetEntity(args.Module);
        if (!TryComp<ModSuitModComponent>(module, out var mod))
            return;
        if (mod.Ejecttick + TimeSpan.FromSeconds(5) >= _timing.CurTime)
            return;
        mod.Ejecttick = _timing.CurTime;
        component.CurrentComplexity -= mod.Complexity;
        if (mod.Active)
            DeactivateModule(uid, module, mod, component);
        _container.Remove(module, component.ModuleContainer);
        Dirty(module, mod);
        Dirty(uid, component);
        _mod.UpdateUserInterface(uid, component);
    }
    private void OnActivate(EntityUid uid, ModSuitComponent component, ModModulActivateMessage args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        var module = GetEntity(args.Module);
        if (!TryComp<ModSuitModComponent>(module, out var mod))
            return;
        if (mod.Active)
            return;
        ActivateModule(uid, module, mod, component);
        Dirty(module, mod);
        Dirty(uid, component);
        _mod.UpdateUserInterface(uid, component);
    }
    private void OnDeactivate(EntityUid uid, ModSuitComponent component, ModModulDeactivateMessage args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        var module = GetEntity(args.Module);
        if (!TryComp<ModSuitModComponent>(module, out var mod))
            return;
        if (!mod.Active)
            return;

        DeactivateModule(uid, module, mod, component);

        Dirty(module, mod);
        Dirty(uid, component);
        _mod.UpdateUserInterface(uid, component);
    }
    private void OnAfterInteract(EntityUid uid, ModSuitModComponent component, ref BeforeRangedInteractEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        if (!TryComp<ModSuitComponent>(args.Target, out var modsuit))
            return;
        if (modsuit.CurrentComplexity + component.Complexity > modsuit.MaxComplexity)
            return;
        _container.Insert(uid, modsuit.ModuleContainer);
        modsuit.CurrentComplexity += component.Complexity;
        if (component.IsInstantlyActive)
            ActivateModule(args.Target.Value, uid, component, modsuit);
        Dirty(uid, component);
        Dirty(args.Target.Value, modsuit);
        _mod.UpdateUserInterface(args.Target.Value, modsuit);
    }
    private void OnGetUIState(EntityUid uid, ModSuitModComponent component, ModModulesUiStateReadyEvent args)
    {
        args.States.Add(GetNetEntity(uid), null);
    }
    public bool ActivateModule(EntityUid modSuit, EntityUid module, ModSuitModComponent component, ModSuitComponent modcomp)
    {
        //ЛЮБЫЕ МАНИПУЛЯЦИИ С ИНТЕРФЕЙСАМИ РАБОТАЮТ ТОЛЬКО ЧЕРЕЗ ЭТИ ДВА МЕТОДА. ДАЖЕ НЕ ПЫТАЙТЕСЬ СДЕЛАТЬ ЭТО ВСЁ ВНЕ ИХ
        var attachedClothings = modcomp.ClothingUids;
        if (component.Slots.Contains("MODcore"))
        {
            EntityManager.AddComponents(modSuit, component.Components);
        }
        component.Active = true;
        foreach (var attached in attachedClothings)
        {
            if (!component.Slots.Contains(attached.Value))
                continue;
            EntityManager.AddComponents(attached.Key, component.Components);
            if (component.RemoveComponents != null)
                EntityManager.RemoveComponents(attached.Key, component.RemoveComponents);
        }

        if (TryComp<PowerCellDrawComponent>(modSuit, out var celldraw))
        {
            modcomp.ModEnergyBaseUsing = (float)Math.Round(modcomp.ModEnergyBaseUsing + component.EnergyUsing, 3);
            var attachedCount = _mod.GetAttachedToggleCount(modSuit, modcomp);
            celldraw.DrawRate = modcomp.ModEnergyBaseUsing * attachedCount;
        }

        Dirty(module, component);
        Dirty(modSuit, modcomp);
        // этот таймер нужен в связи с тем, что обновление интерфейса происходит до того, как на клиент передаёт информацию о включении модуля
        Timer.Spawn(1, () =>
        {
            _mod.UpdateUserInterface(modSuit, modcomp);
        });
        return true;
    }
    public bool DeactivateModule(EntityUid modSuit, EntityUid module, ModSuitModComponent component, ModSuitComponent modcomp)
    {
        //ЛЮБЫЕ МАНИПУЛЯЦИИ С ИНТЕРФЕЙСАМИ РАБОТАЮТ ТОЛЬКО ЧЕРЕЗ ЭТИ ДВА МЕТОДА. ДАЖЕ НЕ ПЫТАЙТЕСЬ СДЕЛАТЬ ЭТО ВСЁ ВНЕ ИХ

        var attachedClothings = modcomp.ClothingUids;
        if (component.Slots.Contains("MODcore"))
        {
            EntityManager.RemoveComponents(modSuit, component.Components);
        }

        foreach (var attached in attachedClothings)
        {
            if (!component.Slots.Contains(attached.Value))
                continue;
            EntityManager.RemoveComponents(attached.Key, component.Components);
            if (component.RemoveComponents != null)
                EntityManager.AddComponents(attached.Key, component.RemoveComponents);
            break;
        }
        component.Active = false;

        if (TryComp<PowerCellDrawComponent>(modSuit, out var celldraw))
        {
            modcomp.ModEnergyBaseUsing = (float)Math.Round(modcomp.ModEnergyBaseUsing - component.EnergyUsing, 3);
            var attachedCount = _mod.GetAttachedToggleCount(modSuit, modcomp);
            celldraw.DrawRate = modcomp.ModEnergyBaseUsing * attachedCount;
        }

        Dirty(module, component);
        Dirty(modSuit, modcomp);
        // этот таймер нужен в связи с тем, что обновление интерфейса происходит до того, как на клиент передаёт информацию о включении модуля
        Timer.Spawn(1, () =>
        {
            _mod.UpdateUserInterface(modSuit, modcomp);
        });
        return true;
    }
}
