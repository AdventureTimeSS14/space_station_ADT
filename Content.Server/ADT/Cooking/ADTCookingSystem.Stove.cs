using System.Numerics;
using Content.Shared.ADT.Cooking;
using Content.Shared.ADT.Cooking.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Destructible;
using Content.Shared.Interaction;
using Content.Shared.Nutrition.Components;
using Content.Shared.Power;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Server.ADT.Cooking;

public sealed partial class ADTCookingSystem
{
    private void InitializeStove()
    {
        SubscribeLocalEvent<ADTStoveComponent, InteractUsingEvent>(OnStoveInteractUsing, before: new[] { typeof(SolutionSpikerSystem) });
        SubscribeLocalEvent<ADTStoveComponent, GetVerbsEvent<AlternativeVerb>>(OnStoveGetVerbs);
        SubscribeLocalEvent<ADTStoveComponent, EntInsertedIntoContainerMessage>(OnStoveInserted);
        SubscribeLocalEvent<ADTStoveComponent, EntRemovedFromContainerMessage>(OnStoveRemoved);
        SubscribeLocalEvent<ADTStoveComponent, BreakageEventArgs>(OnStoveBroken);
        SubscribeLocalEvent<ADTStoveComponent, PowerChangedEvent>(OnStovePowerChanged);

        Subs.BuiEvents<ADTStoveComponent>(ADTStoveUiKey.Key, subs =>
        {
            subs.Event<ADTStoveSetTimerMessage>(OnSetOvenTimer);
        });

        SubscribeNetworkEvent<ADTStoveAltClickEvent>(OnStoveAltClick);
    }

    private void OnStoveBroken(Entity<ADTStoveComponent> ent, ref BreakageEventArgs args)
    {
        if (ent.Comp.Broken)
            return;

        ent.Comp.Broken = true;
        StopOvenTimer(ent, false);

        for (var i = 0; i < ent.Comp.Slots.Count; i++)
        {
            SetHeat(ent, i, ADTStoveHeat.Off, null);
        }

        Dirty(ent);
    }

    private void OnStovePowerChanged(Entity<ADTStoveComponent> ent, ref PowerChangedEvent args)
    {
        var now = _timing.CurTime;
        if (!args.Powered && ent.Comp.OvenTimerEnd is { } end)
        {
            ent.Comp.OvenTimerRemaining = end > now ? end - now : TimeSpan.Zero;
            ent.Comp.OvenTimerEnd = null;
            Dirty(ent);
        }
        else if (args.Powered && ent.Comp.OvenTimerRemaining is { } remaining)
        {
            ent.Comp.OvenTimerEnd = now + remaining;
            ent.Comp.OvenTimerRemaining = null;
            Dirty(ent);
        }

        for (var i = 0; i < ent.Comp.Slots.Count; i++)
        {
            if (GetSlotCookware(ent, i) is not { } cookwareUid || !TryComp<ADTCookwareComponent>(cookwareUid, out var cookware))
                continue;

            if (!HasComp<ADTActiveCookingComponent>(cookwareUid))
                continue;

            var cookwareEnt = new Entity<ADTCookwareComponent>(cookwareUid, cookware);
            if (args.Powered)
                RefreshVisualTiming(cookwareEnt);
            else
                RefreshVisualTiming(cookwareEnt, false);

            UpdateAppearance(cookwareEnt, args.Powered ? null : false);
        }
    }

    private bool CheckBroken(Entity<ADTStoveComponent> ent, EntityUid? user)
    {
        if (!ent.Comp.Broken)
            return false;

        if (user != null)
            _popup.PopupEntity(Loc.GetString("adt-stove-broken"), ent, user.Value);

        return true;
    }

    private void OnStoveAltClick(ADTStoveAltClickEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        var uid = GetEntity(ev.Stove);
        if (!TryComp<ADTStoveComponent>(uid, out var stove))
            return;

        if (!_actionBlocker.CanInteract(user, uid) || !_interaction.InRangeUnobstructed(user, uid))
            return;

        var ent = new Entity<ADTStoveComponent>(uid, stove);
        if (CheckBroken(ent, user))
            return;

        var local = GetLocalClick(uid, GetCoordinates(ev.Click));
        var slot = FindNearestSlot(ent, local, _ => true);
        if (slot == null || stove.Slots[slot.Value].Type != ADTStoveSlotType.Burner)
            return;

        var next = NextHeat(stove.Slots[slot.Value]);
        SetHeat(ent, slot.Value, next, user);

        var slotName = Loc.GetString(stove.Slots[slot.Value].Name, ("number", slot.Value + 1));
        _popup.PopupEntity(Loc.GetString("adt-stove-heat-changed", ("slot", slotName), ("heat", Loc.GetString($"adt-stove-heat-{next.ToString().ToLowerInvariant()}"))), uid, user);
    }

    private void OnSetOvenTimer(Entity<ADTStoveComponent> ent, ref ADTStoveSetTimerMessage args)
    {
        var seconds = Math.Clamp(args.Seconds, 0, ent.Comp.OvenTimerMax);
        if (seconds == 0)
        {
            StopOvenTimer(ent, false);
            return;
        }

        if (CheckBroken(ent, args.Actor))
            return;

        ent.Comp.OvenTimerRemaining = null;
        ent.Comp.OvenTimerEnd = _timing.CurTime + TimeSpan.FromSeconds(seconds);
        Dirty(ent);

        for (var i = 0; i < ent.Comp.Slots.Count; i++)
        {
            if (ent.Comp.Slots[i].Type == ADTStoveSlotType.Oven)
                SetHeat(ent, i, Settings.OvenHeat, args.Actor);
        }
    }

    private void StopOvenTimer(Entity<ADTStoveComponent> ent, bool finished)
    {
        ent.Comp.OvenTimerEnd = null;
        ent.Comp.OvenTimerRemaining = null;
        Dirty(ent);

        for (var i = 0; i < ent.Comp.Slots.Count; i++)
        {
            if (ent.Comp.Slots[i].Type == ADTStoveSlotType.Oven)
                SetHeat(ent, i, ADTStoveHeat.Off, null);
        }

        if (!finished)
            return;

        _audio.PlayPvs(Settings.OvenTimerSound, ent);
        _popup.PopupEntity(Loc.GetString("adt-stove-timer-finished"), ent);
    }

    private void UpdateOvenTimers()
    {
        var now = _timing.CurTime;
        if (now < _nextOvenTimerCheck)
            return;

        _nextOvenTimerCheck = now + TimeSpan.FromSeconds(Settings.UpdateInterval);

        _finishedOvens.Clear();
        var query = EntityQueryEnumerator<ADTStoveComponent>();
        while (query.MoveNext(out var uid, out var stove))
        {
            if (stove.OvenTimerEnd is { } end && now >= end)
                _finishedOvens.Add((uid, stove));
        }

        foreach (var stove in _finishedOvens)
            StopOvenTimer(stove, true);
    }

    private void OnStoveInteractUsing(Entity<ADTStoveComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var local = GetLocalClick(ent, args.ClickLocation);
        var used = args.Used;
        var user = args.User;

        if (TryComp<ADTCookwareComponent>(used, out var usedCookware))
        {
            args.Handled = true;
            if (CheckBroken(ent, user))
                return;

            var slot = FindNearestSlot(ent, local, i => GetSlotCookware(ent, i) == null && SlotAccepts(ent.Comp.Slots[i], usedCookware.Type));
            if (slot == null)
            {
                _popup.PopupEntity(Loc.GetString("adt-stove-no-free-slot"), ent, user);
                return;
            }

            if (!Container.TryGetContainer(ent, ADTStoveComponent.SlotContainerId(slot.Value), out var container))
                return;

            Container.Insert(used, container);
            return;
        }

        var target = FindNearestSlot(ent, local, i => GetSlotCookware(ent, i) != null);
        if (target == null)
            return;

        var cookwareUid = GetSlotCookware(ent, target.Value);
        if (cookwareUid == null || !TryComp<ADTCookwareComponent>(cookwareUid, out var cookware))
            return;

        var cookwareEnt = (cookwareUid.Value, cookware);

        if (HasComp<SolutionTransferComponent>(used) && !IsBowl(used) && !IsRecipeIngredient(used, cookware.Type))
        {
            args.Handled = true;
            PourInto(cookwareEnt, used, user);
            return;
        }

        args.Handled = TryUseOnCookware(cookwareEnt, used, user);
    }

    private void PourInto(Entity<ADTCookwareComponent> cookware, EntityUid source, EntityUid user)
    {
        if (!TryComp<SolutionTransferComponent>(source, out var transfer))
            return;

        if (!_solution.TryGetDrainableSolution(source, out var sourceSoln, out _))
            return;

        if (!_solution.TryGetRefillableSolution(cookware.Owner, out var targetSoln, out _))
            return;

        _solutionTransfer.Transfer(new SolutionTransferData(user, source, sourceSoln.Value, cookware, targetSoln.Value, transfer.TransferAmount));
    }

    private void OnStoveGetVerbs(Entity<ADTStoveComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;
        for (var i = 0; i < ent.Comp.Slots.Count; i++)
        {
            var index = i;
            var slot = ent.Comp.Slots[i];
            var slotName = Loc.GetString(slot.Name, ("number", i + 1));
            if (slot.Type != ADTStoveSlotType.Oven && !ent.Comp.Broken)
            {
                var next = NextHeat(slot);
                args.Verbs.Add(new AlternativeVerb
                {
                    Text = Loc.GetString("adt-stove-verb-heat", ("slot", slotName), ("heat", Loc.GetString($"adt-stove-heat-{next.ToString().ToLowerInvariant()}"))),
                    Category = ADTCookingVerbCategories.StoveHeat,
                    Act = () => SetHeat(ent, index, next, user),
                    Priority = 10 - i,
                });
            }

            var cookwareUid = GetSlotCookware(ent, i);
            if (cookwareUid == null || !TryComp<ADTCookwareComponent>(cookwareUid, out var cookware))
                continue;

            var cookwareEnt = new Entity<ADTCookwareComponent>(cookwareUid.Value, cookware);

            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("adt-stove-verb-take", ("item", Name(cookwareUid.Value)), ("slot", slotName)),
                Category = ADTCookingVerbCategories.StoveTake,
                Act = () => TakeFromSlot(ent, index, user),
                Priority = 5 - i,
            });

            if (cookware.Contents.ContainedEntities.Count > 0 && cookware.Stage != ADTCookingStage.Cooking && cookware.Stage != ADTCookingStage.OnFire)
            {
                args.Verbs.Add(new AlternativeVerb
                {
                    Text = Loc.GetString("adt-cookware-verb-empty-slot", ("slot", slotName)),
                    Category = ADTCookingVerbCategories.StoveTake,
                    Act = () => EmptyCookware(cookwareEnt, user),
                    Priority = -i,
                });
            }

        }
    }

    private void OnStoveInserted(Entity<ADTStoveComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        var index = GetSlotIndex(args.Container.ID);
        if (index == null || index.Value >= ent.Comp.Slots.Count)
            return;

        if (!TryComp<ADTCookwareComponent>(args.Entity, out var cookware))
            return;

        cookware.TargetKey = null;

        if (ent.Comp.Slots[index.Value].Heat != ADTStoveHeat.Off)
            EnsureActive(args.Entity, ent, index.Value);

        if (ent.Comp.Slots[index.Value].Type == ADTStoveSlotType.Oven)
            _audio.PlayPvs(Settings.OvenCloseSound, ent);

        UpdateAppearance((args.Entity, cookware));
    }

    private void OnStoveRemoved(Entity<ADTStoveComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        var index = GetSlotIndex(args.Container.ID);
        if (index == null)
            return;

        if (!TryComp<ADTCookwareComponent>(args.Entity, out var cookware))
            return;

        if (index.Value < ent.Comp.Slots.Count && ent.Comp.Slots[index.Value].Type == ADTStoveSlotType.Oven && !TerminatingOrDeleted(ent))
            _audio.PlayPvs(Settings.OvenOpenSound, ent);

        StopActive((args.Entity, cookware));
    }

    private void TakeFromSlot(Entity<ADTStoveComponent> ent, int index, EntityUid user)
    {
        var cookwareUid = GetSlotCookware(ent, index);
        if (cookwareUid == null)
            return;

        if (TryComp<ADTCookwareComponent>(cookwareUid, out var cookware) && cookware.FireEndTime != null)
        {
            _popup.PopupEntity(Loc.GetString("adt-cookware-on-fire"), ent, user);
            return;
        }

        _hands.PickupOrDrop(user, cookwareUid.Value);
    }

    private void SetHeat(Entity<ADTStoveComponent> ent, int index, ADTStoveHeat heat, EntityUid? user)
    {
        if (index >= ent.Comp.Slots.Count)
            return;

        if (heat != ADTStoveHeat.Off && CheckBroken(ent, user))
            return;

        ent.Comp.Slots[index].Heat = heat;
        Dirty(ent);

        var cookwareUid = GetSlotCookware(ent, index);
        if (cookwareUid == null || !TryComp<ADTCookwareComponent>(cookwareUid, out var cookware))
            return;

        if (heat == ADTStoveHeat.Off)
            StopActive((cookwareUid.Value, cookware));
        else
            EnsureActive(cookwareUid.Value, ent, index);
    }

    private ADTStoveHeat NextHeat(ADTStoveSlot slot)
    {
        if (slot.Type == ADTStoveSlotType.Oven)
            return slot.Heat == ADTStoveHeat.Off ? Settings.OvenHeat : ADTStoveHeat.Off;

        return slot.Heat switch
        {
            ADTStoveHeat.Off => ADTStoveHeat.Low,
            ADTStoveHeat.Low => ADTStoveHeat.Medium,
            ADTStoveHeat.Medium => ADTStoveHeat.High,
            _ => ADTStoveHeat.Off,
        };
    }

    private static bool SlotAccepts(ADTStoveSlot slot, ADTCookwareType type)
    {
        return slot.Type switch
        {
            ADTStoveSlotType.Burner => type is ADTCookwareType.Pan or ADTCookwareType.Pot,
            ADTStoveSlotType.Oven => type == ADTCookwareType.Tray,
            _ => false,
        };
    }

    private Vector2 GetLocalClick(EntityUid stove, Robust.Shared.Map.EntityCoordinates click)
    {
        var world = _transform.ToMapCoordinates(click).Position;
        return Vector2.Transform(world, _transform.GetInvWorldMatrix(stove));
    }

    private static int? FindNearestSlot(Entity<ADTStoveComponent> ent, Vector2 local, Func<int, bool> predicate)
    {
        int? best = null;
        var bestDistance = float.MaxValue;

        for (var i = 0; i < ent.Comp.Slots.Count; i++)
        {
            if (!predicate(i))
                continue;

            var distance = (ent.Comp.Slots[i].Offset - local).LengthSquared();
            if (distance >= bestDistance)
                continue;

            bestDistance = distance;
            best = i;
        }

        return best;
    }

    private static int? GetSlotIndex(string containerId)
    {
        if (!containerId.StartsWith(ADTStoveComponent.SlotContainerPrefix))
            return null;

        if (!int.TryParse(containerId.Substring(ADTStoveComponent.SlotContainerPrefix.Length), out var index))
            return null;

        return index;
    }

    private void PlayDropSound(Entity<ADTCookwareComponent> ent)
    {
        if (!HasComp<ADTActiveCookingComponent>(ent) || !TryGetStoveSlot(ent, out var stove, out _))
            return;

        if (!_solution.TryGetSolution(ent.Owner, ent.Comp.Solution, out _, out var solution) || solution.Volume <= 0)
            return;

        var sound = ent.Comp.Type switch
        {
            ADTCookwareType.Pan => Settings.FryDropSound,
            ADTCookwareType.Pot => Settings.WaterDropSound,
            _ => null,
        };

        _audio.PlayPvs(sound, stove);
    }

    private bool IsBowl(EntityUid uid)
    {
        var proto = MetaData(uid).EntityPrototype?.ID;
        return proto != null && Settings.Bowls.Contains(proto);
    }
}
