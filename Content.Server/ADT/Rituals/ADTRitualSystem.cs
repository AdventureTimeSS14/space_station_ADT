using System.Linq;
using Content.Shared.ADT.AshWalker.Components;
using Content.Shared.ADT.Rituals;
using Content.Shared.DoAfter;
using Content.Shared.Gibbing;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Rituals;

public sealed partial class ADTRitualSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly GibbingSystem _gibbing = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTRitualObjectComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTRitualObjectComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<ADTRitualObjectComponent, ADTRitualStartMessage>(OnStartMessage);
        SubscribeLocalEvent<ADTActiveRitualComponent, ADTRitualDoAfterEvent>(OnDoAfter);
    }

    private void OnMapInit(Entity<ADTRitualObjectComponent> ent, ref MapInitEvent args)
    {
        foreach (var ritual in GetRituals(ent))
        {
            ent.Comp.Charges.TryAdd(ritual.ID, ritual.Charges);
        }
    }

    private void OnInteractHand(Entity<ADTRitualObjectComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!CanUse(ent, args.User))
            return;

        if (!_ui.TryOpenUi(ent.Owner, ADTRitualUiKey.Key, args.User))
            return;

        UpdateUi(ent);
        args.Handled = true;
    }

    private void UpdateUi(Entity<ADTRitualObjectComponent> ent)
    {
        var entries = new List<ADTRitualEntry>();

        foreach (var ritual in GetRituals(ent))
        {
            var charges = ent.Comp.Charges.GetValueOrDefault(ritual.ID, ritual.Charges);
            var until = ent.Comp.Cooldowns.GetValueOrDefault(ritual.ID, TimeSpan.Zero);

            entries.Add(new ADTRitualEntry(ritual.ID, charges, until));
        }

        _ui.SetUiState(ent.Owner, ADTRitualUiKey.Key, new ADTRitualBuiState(entries, ent.Comp.Busy));
    }

    private void OnStartMessage(Entity<ADTRitualObjectComponent> ent, ref ADTRitualStartMessage args)
    {
        var user = args.Actor;

        if (!CanUse(ent, user))
            return;

        if (!_proto.TryIndex(args.Ritual, out var ritual) || !ent.Comp.Categories.Any(ritual.Categories.Contains))
            return;

        TryStart(ent, ritual, user);
        UpdateUi(ent);
    }

    private bool CanUse(Entity<ADTRitualObjectComponent> ent, EntityUid user)
    {
        return ent.Comp.AllowedSpecies.Count == 0 || IsAllowedSpecies(user, ent.Comp.AllowedSpecies);
    }

    private bool IsAllowedSpecies(EntityUid uid, List<ProtoId<SpeciesPrototype>> allowed)
    {
        return TryComp<HumanoidProfileComponent>(uid, out var profile) && allowed.Contains(profile.Species);
    }

    private IEnumerable<ADTRitualPrototype> GetRituals(Entity<ADTRitualObjectComponent> ent)
    {
        foreach (var ritual in _proto.EnumeratePrototypes<ADTRitualPrototype>())
        {
            if (ent.Comp.Categories.Any(ritual.Categories.Contains))
                yield return ritual;
        }
    }

    private void TryStart(Entity<ADTRitualObjectComponent> ent, ADTRitualPrototype ritual, EntityUid user)
    {
        if (ent.Comp.Busy)
        {
            Popup(ent, user, "adt-ritual-busy");
            return;
        }

        if (ent.Comp.Charges.GetValueOrDefault(ritual.ID, ritual.Charges) == 0)
            return;

        if (ent.Comp.Cooldowns.TryGetValue(ritual.ID, out var until) && until > _timing.CurTime)
            return;

        _audio.PlayPvs(ritual.StartSound, ent.Owner);

        if (!TryGatherInvokers(ent, ritual, user, out var invokers))
            return;

        if (!CheckInvokers(ent, ritual, user, invokers))
            return;

        var consumable = new List<EntityUid>();
        var things = GatherThings(ent, ritual, invokers, consumable);

        if (things == null)
        {
            Popup(ent, user, "adt-ritual-need-things");
            return;
        }

        var args = new ADTRitualArgs(ent.Owner, ritual, user, invokers, things);

        foreach (var check in ritual.Checks)
        {
            if (check.Check(EntityManager, args, out var reason))
                continue;

            if (reason != null)
                _popup.PopupEntity(Loc.GetString(reason), ent.Owner, user);

            return;
        }

        var fail = ritual.FailChance;
        var disaster = ritual.DisasterChance;

        foreach (var modifier in ritual.Modifiers)
        {
            modifier.Apply(EntityManager, args, ref fail, ref disaster);
        }

        if (_random.Prob(fail))
        {
            Collapse(ent, ritual, user, invokers, things, consumable, disaster);
            return;
        }

        if (ritual.CastTime <= TimeSpan.Zero)
        {
            Succeed(ent, ritual, user, invokers, things, consumable);
            return;
        }

        BeginCast(ent, ritual, user, invokers, things, consumable, disaster);
    }

    private bool TryGatherInvokers(
        Entity<ADTRitualObjectComponent> ent,
        ADTRitualPrototype ritual,
        EntityUid user,
        out List<EntityUid> invokers)
    {
        invokers = new List<EntityUid> { user };

        if (ritual.ExtraInvokers <= 0)
            return true;

        invokers.Clear();

        var nearby = new HashSet<EntityUid>();
        _lookup.GetEntitiesInRange(Transform(ent.Owner).Coordinates, ritual.FindingRange, nearby);

        foreach (var candidate in nearby)
        {
            if (IsValidInvoker(candidate, ritual))
                invokers.Add(candidate);
        }

        if (invokers.Count >= ritual.ExtraInvokers + 1)
            return true;

        Popup(ent, user, "adt-ritual-need-invokers");
        return false;
    }

    private bool IsValidInvoker(EntityUid uid, ADTRitualPrototype ritual)
    {
        if (HasComp<ADTRitualTotemComponent>(uid))
            return true;

        if (!HasComp<HumanoidProfileComponent>(uid) || _mobState.IsDead(uid))
            return false;

        return ritual.AllowedSpecies.Count == 0 || IsAllowedSpecies(uid, ritual.AllowedSpecies);
    }

    private bool CheckInvokers(
        Entity<ADTRitualObjectComponent> ent,
        ADTRitualPrototype ritual,
        EntityUid user,
        List<EntityUid> invokers)
    {
        if (ritual.ShamanOnly && !IsShaman(user))
        {
            Popup(ent, user, "adt-ritual-shaman-only");
            return false;
        }

        if (ritual.NeededDye != null)
        {
            foreach (var invoker in invokers)
            {
                var wanted = HasComp<ADTRitualTotemComponent>(invoker) ? ritual.TotemDye : ritual.NeededDye;

                if (wanted == null)
                    continue;

                if (!TryComp<ADTDyedComponent>(invoker, out var dyed) || dyed.Dye != wanted)
                {
                    Popup(ent, user, "adt-ritual-no-dye");
                    return false;
                }
            }
        }

        if (ritual.ExtraShamanInvokers > 0)
        {
            var shamans = invokers.Count(IsShaman);

            if (shamans < ritual.ExtraShamanInvokers + 1)
            {
                Popup(ent, user, "adt-ritual-need-shamans");
                return false;
            }
        }

        return true;
    }

    private bool IsShaman(EntityUid uid)
    {
        return TryComp<ADTAshWalkerComponent>(uid, out var walker) && walker.Shaman;
    }

    private List<EntityUid>? GatherThings(
        Entity<ADTRitualObjectComponent> ent,
        ADTRitualPrototype ritual,
        List<EntityUid> invokers,
        List<EntityUid> consumable)
    {
        var used = new List<EntityUid>();

        if (ritual.RequiredThings.Count == 0)
            return used;

        var nearby = new HashSet<EntityUid>();
        _lookup.GetEntitiesInRange(Transform(ent.Owner).Coordinates, ritual.FindingRange, nearby);

        foreach (var required in ritual.RequiredThings)
        {
            var left = required.Amount;

            foreach (var candidate in nearby)
            {
                if (left <= 0)
                    break;

                if (candidate == ent.Owner || invokers.Contains(candidate) || used.Contains(candidate))
                    continue;

                if (!_whitelist.IsValid(required.Whitelist, candidate))
                    continue;

                used.Add(candidate);

                if (required.Consume)
                    consumable.Add(candidate);

                left -= TryComp<StackComponent>(candidate, out var stack) ? stack.Count : 1;
            }

            if (left > 0)
                return null;
        }

        return used;
    }

    private void BeginCast(
        Entity<ADTRitualObjectComponent> ent,
        ADTRitualPrototype ritual,
        EntityUid user,
        List<EntityUid> invokers,
        List<EntityUid> things,
        List<EntityUid> consumable,
        float disaster)
    {
        var active = EnsureComp<ADTActiveRitualComponent>(ent.Owner);
        active.Ritual = ritual.ID;
        active.Invoker = user;
        active.Invokers = invokers;
        active.UsedThings = things;
        active.Consumable = consumable;
        active.DisasterChance = disaster;
        active.ThingPositions.Clear();
        active.Queue.Clear();
        active.Index = 0;

        foreach (var thing in things)
        {
            active.ThingPositions[thing] = Transform(thing).Coordinates;
        }

        foreach (var invoker in invokers)
        {
            if (!HasComp<ADTRitualTotemComponent>(invoker))
                active.Queue.Add(invoker);
        }

        ent.Comp.Busy = true;

        if (!TryCastNext((ent.Owner, active)))
        {
            Cancel(ent.Owner);
            Collapse(ent, ritual, user, invokers, things, consumable, disaster);
        }
    }

    private bool TryCastNext(Entity<ADTActiveRitualComponent> ent)
    {
        if (!_proto.TryIndex(ent.Comp.Ritual, out var ritual))
            return false;

        if (ent.Comp.Index >= ent.Comp.Queue.Count)
            return false;

        var invoker = ent.Comp.Queue[ent.Comp.Index];

        var doAfter = new DoAfterArgs(EntityManager, invoker, ritual.CastTime, new ADTRitualDoAfterEvent(), ent.Owner, ent.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            BlockDuplicate = true,
            CancelDuplicate = true,
        };

        return _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnDoAfter(Entity<ADTActiveRitualComponent> ent, ref ADTRitualDoAfterEvent args)
    {
        if (!TryComp<ADTRitualObjectComponent>(ent.Owner, out var obj))
            return;

        if (!_proto.TryIndex(ent.Comp.Ritual, out var ritual))
            return;

        var invoker = ent.Comp.Invoker;
        var invokers = ent.Comp.Invokers;
        var things = ent.Comp.UsedThings;
        var consumable = ent.Comp.Consumable;
        var disaster = ent.Comp.DisasterChance;

        if (args.Cancelled)
        {
            Cancel(ent.Owner);
            Collapse((ent.Owner, obj), ritual, invoker, invokers, things, consumable, disaster);
            return;
        }

        ent.Comp.Index++;

        if (ent.Comp.Index < ent.Comp.Queue.Count)
        {
            if (TryCastNext(ent))
                return;

            Cancel(ent.Owner);
            Collapse((ent.Owner, obj), ritual, invoker, invokers, things, consumable, disaster);
            return;
        }

        foreach (var (thing, where) in ent.Comp.ThingPositions)
        {
            if (Deleted(thing) || Transform(thing).Coordinates != where)
            {
                Cancel(ent.Owner);
                Collapse((ent.Owner, obj), ritual, invoker, invokers, things, consumable, disaster);
                return;
            }
        }

        if (obj.FinaleEffect is { } finale)
            Spawn(finale, Transform(ent.Owner).Coordinates);

        if (obj.FinaleDelay <= TimeSpan.Zero)
        {
            Cancel(ent.Owner);
            Succeed((ent.Owner, obj), ritual, invoker, invokers, things, consumable);
            return;
        }

        ent.Comp.ResolveAt = _timing.CurTime + obj.FinaleDelay;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTActiveRitualComponent, ADTRitualObjectComponent>();

        while (query.MoveNext(out var uid, out var active, out var obj))
        {
            if (active.ResolveAt is not { } at || now < at)
                continue;

            if (!_proto.TryIndex(active.Ritual, out var ritual))
            {
                Cancel(uid);
                continue;
            }

            var invoker = active.Invoker;
            var invokers = active.Invokers;
            var things = active.UsedThings;
            var consumable = active.Consumable;

            Cancel(uid);
            Succeed((uid, obj), ritual, invoker, invokers, things, consumable);
        }
    }

    private void Cancel(EntityUid uid)
    {
        RemCompDeferred<ADTActiveRitualComponent>(uid);
    }

    private void Succeed(
        Entity<ADTRitualObjectComponent> ent,
        ADTRitualPrototype ritual,
        EntityUid user,
        List<EntityUid> invokers,
        List<EntityUid> things,
        List<EntityUid> consumable)
    {
        var args = new ADTRitualArgs(ent.Owner, ritual, user, invokers, things);

        foreach (var effect in ritual.Effects)
        {
            effect.Effect(EntityManager, args);
        }

        _audio.PlayPvs(ritual.SuccessSound, ent.Owner);
        Popup(ent, user, "adt-ritual-success");

        StartCooldown(ent, ritual);

        if (ritual.Charges != -1 && ent.Comp.Charges.TryGetValue(ritual.ID, out var charges) && charges > 0)
            ent.Comp.Charges[ritual.ID] = charges - 1;

        if (ritual.DeleteThingsOnSuccess)
            DeleteThings(consumable);

        WashDyes(ritual, invokers);

        ent.Comp.Busy = false;
        UpdateUi(ent);
    }

    private void Collapse(
        Entity<ADTRitualObjectComponent> ent,
        ADTRitualPrototype ritual,
        EntityUid user,
        List<EntityUid> invokers,
        List<EntityUid> things,
        List<EntityUid> consumable,
        float disaster)
    {
        _audio.PlayPvs(ritual.FailSound, ent.Owner);
        Popup(ent, user, "adt-ritual-failed");

        StartCooldown(ent, ritual);

        if (_random.Prob(disaster))
        {
            var args = new ADTRitualArgs(ent.Owner, ritual, user, invokers, things);

            foreach (var effect in ritual.DisasterEffects)
            {
                effect.Effect(EntityManager, args);
            }
        }

        if (ritual.DeleteThingsOnFail)
            DeleteThings(consumable);

        ent.Comp.Busy = false;
        UpdateUi(ent);
    }

    private void StartCooldown(Entity<ADTRitualObjectComponent> ent, ADTRitualPrototype ritual)
    {
        ent.Comp.Cooldowns[ritual.ID] = _timing.CurTime + ritual.Cooldown;
    }

    private void DeleteThings(List<EntityUid> things)
    {
        foreach (var thing in things)
        {
            if (Deleted(thing))
                continue;

            if (HasComp<MobStateComponent>(thing))
                _gibbing.Gib(thing);
            else
                QueueDel(thing);
        }
    }

    private void WashDyes(ADTRitualPrototype ritual, List<EntityUid> invokers)
    {
        if (ritual.NeededDye == null)
            return;

        foreach (var invoker in invokers)
        {
            if (!TryComp<ADTDyedComponent>(invoker, out var dyed) || dyed.Dye == null)
                continue;

            dyed.Dye = null;
            Dirty(invoker, dyed);
            _popup.PopupEntity(Loc.GetString("adt-ritual-dye-washed"), invoker, invoker);
        }
    }

    public void AddCharge(Entity<ADTRitualObjectComponent?> ent, ADTRitualPrototype ritual, int amount = 1)
    {
        if (!Resolve(ent, ref ent.Comp, false) || ritual.Charges == -1)
            return;

        ent.Comp.Charges[ritual.ID] = ent.Comp.Charges.GetValueOrDefault(ritual.ID, ritual.Charges) + amount;
    }

    public IEnumerable<ADTRitualPrototype> GetRitualsOf(Entity<ADTRitualObjectComponent> ent) => GetRituals(ent);

    private void Popup(Entity<ADTRitualObjectComponent> ent, EntityUid user, string message)
    {
        _popup.PopupEntity(Loc.GetString(message), ent.Owner, user);
    }
}
