using Content.Shared.ADT.Cooking;
using Content.Shared.ADT.Cooking.Components;
using Content.Shared.ADT.Cooking.Prototypes;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Player;

using Content.Shared.Timing;

namespace Content.Server.ADT.Cooking;

public sealed partial class ADTCookingSystem
{
    private void InitializeCookware()
    {
        SubscribeLocalEvent<ADTCookwareComponent, InteractUsingEvent>(OnCookwareInteractUsing, before: new[] { typeof(SolutionSpikerSystem) });
        SubscribeLocalEvent<ADTCookwareComponent, GetVerbsEvent<AlternativeVerb>>(OnCookwareGetVerbs);
        SubscribeLocalEvent<ADTCookwareComponent, EntRemovedFromContainerMessage>(OnCookwareContentRemoved);
        SubscribeLocalEvent<ADTCookwareComponent, SolutionChangedEvent>(OnCookwareSolutionChanged);
        SubscribeLocalEvent<ADTCookwareComponent, EntityPausedEvent>(OnCookwarePaused);
        SubscribeLocalEvent<ADTCookwareComponent, EntityUnpausedEvent>(OnCookwareUnpaused);
    }

    private void OnCookwarePaused(Entity<ADTCookwareComponent> ent, ref EntityPausedEvent args)
    {
        RefreshVisualTiming(ent, false);
    }

    private void OnCookwareUnpaused(Entity<ADTCookwareComponent> ent, ref EntityUnpausedEvent args)
    {
        RefreshVisualTiming(ent);
    }

    private void OnCookwareInteractUsing(Entity<ADTCookwareComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<ADTCookwareComponent>(args.Used))
            return;

        if (HasComp<SolutionTransferComponent>(args.Used) && !IsBowl(args.Used) && !IsRecipeIngredient(args.Used, ent.Comp.Type))
            return;

        args.Handled = TryUseOnCookware(ent, args.Used, args.User);
    }

    private void OnCookwareGetVerbs(Entity<ADTCookwareComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (ent.Comp.Contents.ContainedEntities.Count == 0 || ent.Comp.Stage == ADTCookingStage.OnFire)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("adt-cookware-verb-empty"),
            Act = () => EmptyCookware(ent, user),
        });
    }

    private void OnCookwareContentRemoved(Entity<ADTCookwareComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ADTCookwareComponent.ContainerId)
            return;

        if (ent.Comp.Stage is ADTCookingStage.Done or ADTCookingStage.Overcooked && TryComp<ADTDishComponent>(args.Entity, out var dish))
        {
            dish.Burn = Math.Max(dish.Burn, GetBurnFraction(ent.Comp));
            Dirty(args.Entity, dish);
        }

        ent.Comp.CookResults.Remove(GetNetEntity(args.Entity));
        if (ent.Comp.LargeItems.Remove(GetNetEntity(args.Entity)))
            Dirty(ent);

        if (!HasResult(ent) && ent.Comp.Stage >= ADTCookingStage.Done && ent.Comp.Stage != ADTCookingStage.OnFire)
            ResetCooking(ent);
        else if (ent.Comp.Stage == ADTCookingStage.Cooking && !TerminatingOrDeleted(ent) && !TerminatingOrDeleted(args.Entity))
            UpdateTarget(ent, GetMedium(ent), Settings);
    }

    private bool HasResult(Entity<ADTCookwareComponent> ent)
    {
        var comp = ent.Comp;
        if (comp.Contents.ContainedEntities.Count > 0)
            return true;

        if (comp.SoupResult == null)
            return false;

        return _solution.TryGetSolution(ent.Owner, comp.Solution, out _, out var soup) && soup.Volume > 0;
    }

    private static float GetBurnFraction(ADTCookwareComponent comp)
    {
        if (comp.BurnTime <= 0)
            return 0f;

        return Math.Clamp((comp.CookTime - comp.TargetTime) / comp.BurnTime, 0f, 1f);
    }

    private void OnCookwareSolutionChanged(Entity<ADTCookwareComponent> ent, ref SolutionChangedEvent args)
    {
        if (ent.Comp.Stage >= ADTCookingStage.Done
            && ent.Comp.Stage != ADTCookingStage.OnFire
            && !TerminatingOrDeleted(ent)
            && !HasResult(ent))
        {
            ResetCooking(ent);
            return;
        }

        UpdateAppearance(ent);
    }

    private bool TryUseOnCookware(Entity<ADTCookwareComponent> ent, EntityUid used, EntityUid user)
    {
        var settings = Settings;
        var comp = ent.Comp;

        if (comp.Stage == ADTCookingStage.OnFire)
        {
            _popup.PopupEntity(Loc.GetString("adt-cookware-on-fire"), ent, user);
            return true;
        }

        if (IsBowl(used))
            return TryLadle(ent, used, user);

        if (_tag.HasTag(used, settings.FlipTag))
        {
            if (comp.Type == ADTCookwareType.Pan && TryUseTool(used))
                TryFlip(ent, user);

            return true;
        }

        if (_tag.HasTag(used, settings.StirTag))
        {
            if (comp.Type == ADTCookwareType.Pot && TryUseTool(used))
                TryStir(ent, user);

            return true;
        }

        var proto = MetaData(used).EntityPrototype?.ID;
        if (proto != null && comp.Type == ADTCookwareType.Pan && settings.MeltIntoMedium.Contains(proto))
            return TryMelt(ent, used, user);

        if (TryComp<ADTCookwareCrackableComponent>(used, out var crackable) && crackable.Cookware.Contains(comp.Type))
            return TryCrack(ent, (used, crackable), user);

        if (!HasComp<EdibleComponent>(used) && !IsRecipeIngredient(used, comp.Type))
            return false;

        if (comp.Stage >= ADTCookingStage.Done)
        {
            if (comp.Type == ADTCookwareType.Pot && comp.Stage == ADTCookingStage.Done && comp.SoupResult != null)
                return TryAddToFinishedSoup(ent, used, user);

            _popup.PopupEntity(Loc.GetString("adt-cookware-take-result-first"), ent, user);
            return true;
        }

        if (comp.Contents.ContainedEntities.Count >= comp.MaxItems)
        {
            _popup.PopupEntity(Loc.GetString("adt-cookware-full"), ent, user);
            return true;
        }

        if (!Container.Insert(used, comp.Contents))
            return false;

        _popup.PopupEntity(Loc.GetString("adt-cookware-added", ("item", used), ("cookware", ent.Owner)), ent, user);
        PlayDropSound(ent);
        UpdateAppearance(ent);
        return true;
    }

    private bool TryCrack(Entity<ADTCookwareComponent> ent, Entity<ADTCookwareCrackableComponent> crackable, EntityUid user)
    {
        var comp = ent.Comp;
        if (comp.Stage >= ADTCookingStage.Done)
        {
            _popup.PopupEntity(Loc.GetString("adt-cookware-take-result-first"), ent, user);
            return true;
        }

        if (comp.Contents.ContainedEntities.Count >= comp.MaxItems)
        {
            _popup.PopupEntity(Loc.GetString("adt-cookware-full"), ent, user);
            return true;
        }

        Solution? content = null;
        if (TryComp<EdibleComponent>(crackable, out var edible) && _solution.TryGetSolution(crackable.Owner, edible.Solution, out _, out var solution))
            content = solution.Clone();

        var result = EntityManager.SpawnInContainerOrDrop(crackable.Comp.Result, ent, ADTCookwareComponent.ContainerId);
        if (content != null)
            SetFood(result, content);

        var coords = Transform(user).Coordinates;
        var trash = crackable.Comp.Trash;
        var sound = crackable.Comp.Sound;
        Del(crackable);

        if (trash != null)
            _hands.PickupOrDrop(user, Spawn(trash.Value, coords));

        _audio.PlayPvs(sound, ent);
        UpdateAppearance(ent);
        return true;
    }

    private bool TryMelt(Entity<ADTCookwareComponent> ent, EntityUid used, EntityUid user)
    {
        if (ent.Comp.Stage >= ADTCookingStage.Done)
        {
            _popup.PopupEntity(Loc.GetString("adt-cookware-take-result-first"), ent, user);
            return true;
        }

        if (!TryComp<EdibleComponent>(used, out var edible))
            return false;

        if (!_solution.TryGetSolution(used, edible.Solution, out var sourceSoln, out var source))
            return false;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.Solution, out var targetSoln, out var target))
            return false;

        if (target.AvailableVolume < source.Volume)
        {
            _popup.PopupEntity(Loc.GetString("adt-cookware-full"), ent, user);
            return true;
        }

        var split = _solution.SplitSolution(sourceSoln.Value, source.Volume);
        _solution.TryAddSolution(targetSoln.Value, split);
        _popup.PopupEntity(Loc.GetString("adt-cookware-melted", ("item", used)), ent, user);
        QueueDel(used);
        return true;
    }

    private bool TryAddToFinishedSoup(Entity<ADTCookwareComponent> ent, EntityUid used, EntityUid user)
    {
        if (ent.Comp.Addons >= ent.Comp.MaxAddons)
        {
            _popup.PopupEntity(Loc.GetString("adt-cookware-addons-full"), ent, user);
            return true;
        }

        if (!TryComp<EdibleComponent>(used, out var edible) || !_solution.TryGetSolution(used, edible.Solution, out _, out var source))
            return false;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.Solution, out var potSoln, out var pot))
            return false;

        var added = source.Clone();
        if (pot.AvailableVolume < added.Volume)
            _solution.SetCapacity(potSoln.Value, pot.Volume + added.Volume);

        _solution.TryAddSolution(potSoln.Value, added);

        if (TryComp<FlavorProfileComponent>(used, out var flavor))
        {
            foreach (var f in flavor.Flavors)
            {
                ent.Comp.SoupFlavors.Add(f);
            }
        }

        ent.Comp.Addons++;
        _popup.PopupEntity(Loc.GetString("adt-cookware-addon-added", ("item", used)), ent, user);
        QueueDel(used);
        return true;
    }

    private bool TryLadle(Entity<ADTCookwareComponent> ent, EntityUid bowl, EntityUid user)
    {
        var comp = ent.Comp;
        if (comp.Type != ADTCookwareType.Pot)
            return false;

        if (comp.SoupResult is not { } soupProto
            || comp.Stage < ADTCookingStage.Done
            || comp.Stage == ADTCookingStage.OnFire
            || !_solution.TryGetSolution(ent.Owner, comp.Solution, out var potSoln, out var pot)
            || pot.Volume <= 0)
        {
            _popup.PopupEntity(Loc.GetString("adt-cookware-nothing-to-ladle"), ent, user);
            return true;
        }

        var bowlHasItems = TryComp<ADTPlateComponent>(bowl, out var bowlPlate) && bowlPlate.Contents.ContainedEntities.Count > 0;
        if (bowlHasItems || _solution.TryGetDrainableSolution(bowl, out _, out var bowlSolution) && bowlSolution.Volume > 0)
        {
            _popup.PopupEntity(Loc.GetString("adt-cookware-bowl-not-empty"), ent, user);
            return true;
        }

        var portions = Math.Max(1, comp.SoupPortions);
        var portionSolution = _solution.SplitSolution(potSoln.Value, pot.Volume / portions);
        comp.SoupPortions = Math.Max(0, portions - 1);

        var portion = EntityManager.SpawnNextToOrDrop(soupProto, bowl);
        QueueDel(bowl);
        SetFood(portion, portionSolution);

        if (comp.SoupName != null)
            _metaData.SetEntityName(portion, comp.SoupName);

        if (TryComp<FlavorProfileComponent>(portion, out var portionFlavor))
        {
            foreach (var f in comp.SoupFlavors)
            {
                portionFlavor.Flavors.Add(f);
            }
        }

        if (comp.SoupQuality != null)
            _dish.SetDish(portion, ADTCookingMethod.Boiled, comp.SoupQuality.Value);

        _hands.PickupOrDrop(user, portion);
        _popup.PopupEntity(Loc.GetString("adt-cookware-ladled", ("item", portion)), ent, user);

        if (comp.SoupPortions == 0 || pot.Volume <= 0)
        {
            _solution.RemoveAllSolution(potSoln.Value);
            ResetCooking(ent);
        }

        return true;
    }

    private void EmptyCookware(Entity<ADTCookwareComponent> ent, EntityUid user)
    {
        if (ent.Comp.Stage == ADTCookingStage.OnFire)
            return;

        foreach (var item in new List<EntityUid>(ent.Comp.Contents.ContainedEntities))
        {
            Container.Remove(item, ent.Comp.Contents);
            _hands.PickupOrDrop(user, item);
        }
    }

    private bool TryUseTool(EntityUid tool)
    {
        if (!HasComp<UseDelayComponent>(tool))
            return true;

        if (_useDelay.IsDelayed(tool))
            return false;

        _useDelay.TryResetDelay(tool);
        return true;
    }

    private void TryFlip(Entity<ADTCookwareComponent> ent, EntityUid user)
    {
        var comp = ent.Comp;
        if (comp.Type != ADTCookwareType.Pan || comp.Stage != ADTCookingStage.Cooking)
            return;

        if (comp.FlipDone)
        {
            _popup.PopupEntity(Loc.GetString("adt-cookware-flip-already"), user, user);
            return;
        }

        if (!comp.FlipWindowOpen)
        {
            _popup.PopupEntity(Loc.GetString("adt-cookware-flip-wrong-time"), user, user);
            return;
        }

        comp.FlipDone = true;
        comp.FlipWindowOpen = false;
        _audio.PlayPvs(Settings.FlipSound, ent);
        RaiseNetworkEvent(new ADTCookwareFlippedEvent(GetNetEntity(ent)), Filter.Pvs(ent.Owner, entityManager: EntityManager));
        _popup.PopupEntity(Loc.GetString("adt-cookware-flip-success"), user, user);
    }

    private void TryStir(Entity<ADTCookwareComponent> ent, EntityUid user)
    {
        var comp = ent.Comp;
        if (comp.Type != ADTCookwareType.Pot || comp.Stage != ADTCookingStage.Cooking)
            return;

        _audio.PlayPvs(Settings.StirSound, ent);

        if (comp.StirsDone >= comp.StirsNeeded || comp.CookTime > comp.StirWindowEnd)
        {
            _popup.PopupEntity(Loc.GetString("adt-cookware-stir-idle"), user, user);
            return;
        }

        comp.StirsDone++;
        comp.StirWindowEnd = comp.CookTime;
        _popup.PopupEntity(Loc.GetString("adt-cookware-stir-success"), user, user);
    }

    private void ResetMinigames(ADTCookwareComponent comp)
    {
        comp.FlipDone = false;
        comp.FlipWindowOpen = false;
        comp.FlipAnnounced = false;
        comp.StirsNeeded = 0;
        comp.StirsDone = 0;
        comp.NextStirAt = 0;
        comp.StirWindowEnd = 0;
    }

    private void UpdateMinigames(Entity<ADTCookwareComponent> ent, EntityUid popupTarget, ADTCookingSettingsPrototype settings)
    {
        var comp = ent.Comp;
        if (comp.TargetTime <= 0)
            return;

        if (comp.Type == ADTCookwareType.Pan)
        {
            var fraction = comp.CookTime / comp.TargetTime;
            if (!comp.FlipDone && !comp.FlipAnnounced && fraction >= settings.FlipWindowStart)
            {
                comp.FlipAnnounced = true;
                comp.FlipWindowOpen = true;
                _popup.PopupEntity(Loc.GetString("adt-cookware-flip-now", ("cookware", ent.Owner)), popupTarget);
            }

            if (comp.FlipWindowOpen && fraction > settings.FlipWindowEnd)
                comp.FlipWindowOpen = false;
        }

        if (comp.Type == ADTCookwareType.Pot)
        {
            if (comp.NextStirAt <= 0)
                comp.NextStirAt = settings.StirInterval;

            if (comp.CookTime >= comp.NextStirAt && comp.CookTime + settings.StirInterval < comp.TargetTime)
            {
                comp.StirsNeeded++;
                comp.StirWindowEnd = comp.CookTime + settings.StirWindow;
                comp.NextStirAt = comp.CookTime + settings.StirInterval;
                _popup.PopupEntity(Loc.GetString("adt-cookware-stir-now", ("cookware", ent.Owner)), popupTarget);
            }
        }
    }

    private ADTCookingMediumPrototype? GetMedium(Entity<ADTCookwareComponent> ent)
    {
        var method = MethodFor(ent.Comp.Type);
        if (method == ADTCookingMethod.Baked || method == ADTCookingMethod.Raw)
            return null;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.Solution, out _, out var solution))
            return null;

        ADTCookingMediumPrototype? best = null;
        var bestAmount = 0f;
        foreach (var medium in _proto.EnumeratePrototypes<ADTCookingMediumPrototype>())
        {
            if (medium.Method != method)
                continue;

            var amount = solution.GetTotalPrototypeQuantity(medium.Reagent);
            if (amount < medium.MinAmount || amount.Float() <= bestAmount)
                continue;

            best = medium;
            bestAmount = amount.Float();
        }

        return best;
    }

    private void UpdateAppearance(Entity<ADTCookwareComponent> ent, bool? activeOverride = null)
    {
        var comp = ent.Comp;
        var color = Color.Transparent;
        var fill = 0f;
        var batterColor = Color.Transparent;
        var batterVisible = false;

        if (_solution.TryGetSolution(ent.Owner, comp.Solution, out _, out var solution) && solution.Volume > 0)
        {
            var liquid = solution;
            if (comp.Type != ADTCookwareType.Pot)
            {
                var settings = Settings;
                liquid = new Solution();
                var batter = new Solution();
                foreach (var reagent in solution.Contents)
                {
                    var id = reagent.Reagent.Prototype;
                    if (IsMediumReagent(id))
                        liquid.AddReagent(reagent.Reagent, reagent.Quantity);
                    else if (!IsSeasoningReagent(id, settings))
                        batter.AddReagent(reagent.Reagent, reagent.Quantity);
                }

                if (batter.Volume >= settings.BatterMinVolume)
                {
                    batterVisible = true;
                    batterColor = batter.GetColor(_proto);
                }
            }

            if (liquid.Volume > 0)
            {
                color = liquid.GetColor(_proto);
                fill = solution.MaxVolume > 0 ? liquid.Volume.Float() / solution.MaxVolume.Float() : 0f;
            }
        }

        var active = activeOverride ?? (TryComp<ADTActiveCookingComponent>(ent, out var activeComp) && _power.IsPowered(activeComp.Stove));
        var effect = comp.Stage switch
        {
            ADTCookingStage.OnFire => ADTCookwareEffect.Fire,
            ADTCookingStage.Burnt => active ? ADTCookwareEffect.Smoke : ADTCookwareEffect.None,
            ADTCookingStage.Overcooked => active ? ADTCookwareEffect.Smoke : ADTCookwareEffect.None,
            _ when !active => ADTCookwareEffect.None,
            _ when comp.Type == ADTCookwareType.Pot && (fill > 0f || comp.Stage != ADTCookingStage.Idle) => ADTCookwareEffect.Steam,
            _ when comp.Type == ADTCookwareType.Pan && (fill > 0f || batterVisible || comp.Contents.ContainedEntities.Count > 0) => ADTCookwareEffect.Sizzle,
            _ => ADTCookwareEffect.None,
        };

        _appearance.SetData(ent, ADTCookwareVisuals.MediumColor, color);
        _appearance.SetData(ent, ADTCookwareVisuals.MediumFill, fill);
        _appearance.SetData(ent, ADTCookwareVisuals.Effect, effect);
        _appearance.SetData(ent, ADTCookwareVisuals.BatterColor, batterColor);
        _appearance.SetData(ent, ADTCookwareVisuals.BatterVisible, batterVisible);
    }

    private bool IsMediumReagent(string reagent)
    {
        foreach (var medium in _proto.EnumeratePrototypes<ADTCookingMediumPrototype>())
        {
            if (medium.Reagent.Id == reagent)
                return true;
        }

        return false;
    }

    private static bool IsSeasoningReagent(string reagent, ADTCookingSettingsPrototype settings)
    {
        if (settings.SpiceReagents.Contains(reagent) || settings.DressingReagents.Contains(reagent))
            return true;

        foreach (var group in settings.FlavorGroups)
        {
            if (group.Reagents.Contains(reagent))
                return true;
        }

        return false;
    }
}
