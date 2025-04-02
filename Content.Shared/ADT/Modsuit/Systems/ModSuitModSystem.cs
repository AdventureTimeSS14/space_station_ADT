using Content.Shared.Containers.ItemSlots;
using Content.Shared.Clothing;
using Content.Shared.Wires;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.ModSuits;


public sealed class ModSuitModSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ModSuitSystem _mod = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModSuitModComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ModSuitModComponent, ModModulesUiStateReadyEvent>(OnGetUIState);
        SubscribeLocalEvent<ModSuitComponent, ModModuleRemoveMessage>(OnEject);
        SubscribeLocalEvent<ModSuitComponent, ModModulActivateMessage>(OnActivate);
    }
    private void OnEject(EntityUid uid, ModSuitComponent component, ModModuleRemoveMessage args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        var module = GetEntity(args.Module);
        if (!TryComp<ModSuitModComponent>(module, out var mod))
            return;
        if (!mod.Inserted)
            return;
        component.CurrentComplexity -= mod.Complexity;
        _container.Remove(module, component.ModuleContainer);
        mod.Inserted = false;
        Dirty(module, mod);
        Dirty(uid, component);
        _mod.UpdateUserInterface(uid);
    }
    private void OnActivate(EntityUid uid, ModSuitComponent component, ModModulActivateMessage args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        var module = GetEntity(args.Module);
        var modsuit = GetEntity(args.Modsuit);
        if (!TryComp<ModSuitModComponent>(module, out var mod))
            return;

        ActivateModule(modsuit, mod, component);

        mod.Active = true;
        Dirty(module, mod);
        Dirty(uid, component);
        _mod.UpdateUserInterface(uid);
    }
    private void OnAfterInteract(EntityUid uid, ModSuitModComponent component, AfterInteractEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;
        if (component.Inserted)
            return;
        if (!TryComp<ModSuitComponent>(args.Target, out var modsuit))
            return;
        if (modsuit.CurrentComplexity + component.Complexity > modsuit.MaxComplexity)
            return;
        _container.Insert(uid, modsuit.ModuleContainer);
        component.Inserted = true;
        modsuit.CurrentComplexity += 1;
        Dirty(uid, component);
        Dirty(args.Target.Value, modsuit);
        _mod.UpdateUserInterface(args.Target.Value, modsuit);
    }
    private void OnGetUIState(EntityUid uid, ModSuitModComponent component, ModModulesUiStateReadyEvent args)
    {
        args.States.Add(GetNetEntity(uid), null);
    }
    public bool ActivateModule(EntityUid modSuit, ModSuitModComponent component, ModSuitComponent modcomp)
    {
        var container = modcomp.Container;
        if (container == null)
            return false;

        var attachedClothings = modcomp.ClothingUids;
        if (component.Slots.Contains("MODcore"))
        {
            EntityManager.AddComponents(modSuit, component.Components);
        }

        foreach (var attached in attachedClothings)
        {
            if (!container.Contains(attached.Key))
                continue;
            if (!component.Slots.Contains(attached.Value))
                continue;
            EntityManager.AddComponents(attached.Key, component.Components);
            if (component.RemoveComponents != null)
                EntityManager.RemoveComponents(attached.Key, component.RemoveComponents);
            break;
        }
        Dirty(modSuit, component);

        return true;
    }
    public void DeactivateModule(EntityUid modSuit, ModSuitModComponent component)
    {
        if (!TryComp<ModSuitComponent>(modSuit, out var modsuit))
        {
            return;
        }
        var container = modsuit.Container;
        if (container == null)
            return;

        var attachedClothings = modsuit.ClothingUids;
        if (component.Slots.Contains("MODcore"))
        {
            EntityManager.RemoveComponents(modSuit, component.Components);
        }

        foreach (var attached in attachedClothings)
        {
            if (!container.Contains(attached.Key))
                continue;
            if (!component.Slots.Contains(attached.Value))
                continue;
            EntityManager.RemoveComponents(attached.Key, component.Components);
            if (component.RemoveComponents != null)
                EntityManager.AddComponents(attached.Key, component.RemoveComponents);
            break;
        }
    }
}
