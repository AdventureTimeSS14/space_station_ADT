using System;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.Actions;
using Content.Shared.Clothing.Components;
using Content.Shared.Wires;

namespace Content.Shared.ADT.ModSuits;

public sealed partial class ModSuitSystem
{
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    private void InitializeModules()
    {
        SubscribeLocalEvent<ModSuitModComponent, BeforeRangedInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ModSuitModComponent, ModModulesUiStateReadyEvent>(OnGetUIState);
        SubscribeLocalEvent<ModSuitModComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<ModSuitComponent, ModModuleRemoveMessage>(OnEject);
        SubscribeLocalEvent<ModSuitComponent, ModModuleActivateMessage>(OnActivate);
        SubscribeLocalEvent<ModSuitComponent, ModModuleDeactivateMessage>(OnDeactivate);
    }

    private void AddComponentsSafe(EntityUid uid, ComponentRegistry components, string entityName)
    {
        try
        {
            EntityManager.AddComponents(uid, components, removeExisting: false);
        }
        catch (Exception ex)
        {
            Log.Debug($"Failed to add components to {entityName} (may already exist): {ex.Message}");
        }
    }

    private void OnEject(Entity<ModSuitComponent> ent, ref ModModuleRemoveMessage args)
    {
        var module = GetEntity(args.Module);
        if (!TryComp<ModSuitModComponent>(module, out var mod))
            return;

        if (TryComp<WiresPanelComponent>(ent, out var panel) && !panel.Open)
            return;

        if (ent.Comp.UserName != null && (!_id.TryFindIdCard(args.Actor, out var id) || ent.Comp.UserName != id.Comp.FullName))
        {
            _popupSystem.PopupPredicted(Loc.GetString("modsuit-locked-popup"), args.Actor, args.Actor);
            return;
        }

        ent.Comp.CurrentComplexity -= mod.Complexity;

        if (mod.Active)
            DeactivateModule(ent, (module, mod));

        _containerSystem.Remove(module, ent.Comp.ModuleContainer);
        _audio.PlayPredicted(ent.Comp.EjectModuleSound, ent, args.Actor);

        Dirty(module, mod);
        UpdateUserInterface(ent, ent.Comp);
    }

    private void OnActivate(Entity<ModSuitComponent> ent, ref ModModuleActivateMessage args)
    {
        var module = GetEntity(args.Module);
        if (!TryComp<ModSuitModComponent>(module, out var mod))
            return;

        if (mod.Active)
            return;

        ActivateModule(ent, (module, mod));
    }

    private void OnDeactivate(Entity<ModSuitComponent> ent, ref ModModuleDeactivateMessage args)
    {
        var module = GetEntity(args.Module);
        if (!TryComp<ModSuitModComponent>(module, out var mod))
            return;

        if (!mod.Active)
            return;

        DeactivateModule(ent, (module, mod));
    }

    private void OnAfterInteract(Entity<ModSuitModComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (!TryComp<ModSuitComponent>(args.Target, out var modsuit))
            return;

        if (!TryComp<WiresPanelComponent>(args.Target, out var panel) || !panel.Open)
            return;

        if (modsuit.CurrentComplexity + ent.Comp.Complexity > modsuit.MaxComplexity)
            return;

        _containerSystem.Insert(ent.Owner, modsuit.ModuleContainer);
        _audio.PlayPredicted(modsuit.InsertModuleSound, args.Target.Value, args.User);

        modsuit.CurrentComplexity += ent.Comp.Complexity;

        if (ent.Comp.IsInstantlyActive)
            ActivateModule((args.Target.Value, modsuit), ent);

        Dirty(ent);
        UpdateUserInterface(args.Target.Value, modsuit);
    }

    private void OnGetUIState(EntityUid uid, ModSuitModComponent component, ModModulesUiStateReadyEvent args)
    {
        args.States.Add(GetNetEntity(uid), null);
    }

    public void ActivateModule(Entity<ModSuitComponent> suit, Entity<ModSuitModComponent> module)
    {
        module.Comp.Active = true;
        Dirty(module);

        if (!module.Comp.RequiresFullEquip || GetPartsToggleStatus(suit.Owner, suit.Comp) == ModSuitAttachedStatus.AllToggled)
            ApplyModuleComponents(suit, module);

        if (_netMan.IsServer)
            UpdateUserInterface(suit, suit.Comp);

        UpdateCellDraw(suit);
    }

    public void DeactivateModule(Entity<ModSuitComponent> suit, Entity<ModSuitModComponent> module)
    {
        module.Comp.Active = false;
        Dirty(module);

        RemoveModuleComponents(suit, module);

        if (_netMan.IsServer)
            UpdateUserInterface(suit, suit.Comp);

        UpdateCellDraw(suit);
    }

    private void ApplyModuleComponents(Entity<ModSuitComponent> suit, Entity<ModSuitModComponent> module)
    {
        if (!_netMan.IsServer || module.Comp.ComponentsApplied)
            return;

        module.Comp.ComponentsApplied = true;

        if (module.Comp.Components.TryGetValue("MODcore", out var defaultComps))
        {
            AddComponentsSafe(suit, defaultComps, ToPrettyString(suit));
            if (suit.Comp.TempUser != null)
                UpdateActions(suit, suit.Comp.TempUser.Value);
        }

        foreach (var attached in suit.Comp.ClothingUids)
        {
            var part = GetEntity(attached.Key);

            if (!Exists(part))
                continue;

            if (module.Comp.Components.TryGetValue(attached.Value, out var comps))
                AddComponentsSafe(part, comps, ToPrettyString(part));

            if (module.Comp.RemoveComponents != null && module.Comp.RemoveComponents.TryGetValue(attached.Value, out var remComps))
            {
                try
                {
                    EntityManager.RemoveComponents(part, remComps);
                }
                catch (Exception ex)
                {
                    Log.Warning($"Failed to remove components from part {ToPrettyString(part)}: {ex.Message}");
                }
            }

            if (suit.Comp.TempUser != null)
                UpdateActions(part, suit.Comp.TempUser.Value);
        }
    }

    private void RemoveModuleComponents(Entity<ModSuitComponent> suit, Entity<ModSuitModComponent> module)
    {
        if (!_netMan.IsServer || !module.Comp.ComponentsApplied)
            return;

        module.Comp.ComponentsApplied = false;

        if (module.Comp.Components.TryGetValue("MODcore", out var defaultComps))
        {
            try
            {
                EntityManager.RemoveComponents(suit, defaultComps);
                if (suit.Comp.TempUser != null)
                    UpdateActions(suit, suit.Comp.TempUser.Value);
            }
            catch (Exception ex)
            {
                Log.Warning($"Failed to remove MODcore components from suit {ToPrettyString(suit)}: {ex.Message}");
            }
        }

        foreach (var attached in suit.Comp.ClothingUids)
        {
            var part = GetEntity(attached.Key);

            if (!Exists(part))
                continue;

            if (module.Comp.Components.TryGetValue(attached.Value, out var comps))
            {
                try
                {
                    EntityManager.RemoveComponents(part, comps);
                }
                catch (Exception ex)
                {
                    Log.Warning($"Failed to remove components from part {ToPrettyString(part)}: {ex.Message}");
                }
            }

            if (module.Comp.RemoveComponents != null && module.Comp.RemoveComponents.TryGetValue(attached.Value, out var remComps))
                AddComponentsSafe(part, remComps, ToPrettyString(part));

            if (suit.Comp.TempUser != null)
                UpdateActions(part, suit.Comp.TempUser.Value);
        }
    }

    /// <summary>
    ///     Applies or removes a full-equip module's components so they are present only while it is toggled on
    ///     (<see cref="ModSuitModComponent.Active"/>) and the suit is fully equipped.
    /// </summary>
    private void ReconcileFullEquip(Entity<ModSuitComponent> suit, Entity<ModSuitModComponent> module)
    {
        var shouldApply = module.Comp.Active
            && GetPartsToggleStatus(suit.Owner, suit.Comp) == ModSuitAttachedStatus.AllToggled;

        if (shouldApply)
            ApplyModuleComponents(suit, module);
        else
            RemoveModuleComponents(suit, module);
    }

    /// <summary>
    ///     Re-reconciles every full-equip module. Call whenever the deployed-part set changes.
    /// </summary>
    private void RefreshFullEquipModules(Entity<ModSuitComponent> suit)
    {
        foreach (var moduleUid in suit.Comp.ModuleContainer.ContainedEntities)
        {
            if (TryComp<ModSuitModComponent>(moduleUid, out var mod) && mod.RequiresFullEquip)
                ReconcileFullEquip(suit, (moduleUid, mod));
        }
    }

    public string GetColor(ExamineColor color, string text)
    {
        var colorCode = color switch
        {
            ExamineColor.Red => "red",
            ExamineColor.Yellow => "yellow",
            _ => "green"
        };

        return $"[color={colorCode}]{text}[/color]";
    }

    private void OnExamine(EntityUid uid, ModSuitModComponent mod, ref ExaminedEvent args)
    {
        var complexityColor = mod.Complexity switch
        {
            > 2 => ExamineColor.Red,
            > 1 => ExamineColor.Yellow,
            _ => ExamineColor.Green
        };

        var energyColor = mod.EnergyUsing switch
        {
            > 0.2f => ExamineColor.Red,
            > 0.1f => ExamineColor.Yellow,
            _ => ExamineColor.Green
        };

        args.PushMarkup(Loc.GetString("modsuit-mod-description-complexity",
            ("complexity", GetColor(complexityColor, mod.Complexity.ToString("0")))));

        args.PushMarkup(Loc.GetString("modsuit-mod-description-energy",
            ("energy", GetColor(energyColor, mod.EnergyUsing.ToString("0.0#")))));
    }

    private void UpdateActions(EntityUid part, EntityUid user)
    {
        if (!TryComp<ClothingComponent>(part, out var clothing) || clothing.InSlotFlag == null)
            return;

        _actionsSystem.RemoveProvidedActions(user, part);

        var ev = new GetItemActionsEvent(
            _actionContainer,
            user,
            part,
            clothing.InSlotFlag.Value);

        RaiseLocalEvent(part, ev);

        if (ev.Actions.Count > 0)
            _actionsSystem.GrantActions(user, ev.Actions, part);
    }
}
