using Content.Shared.ADT.Rituals;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Rituals;

public sealed class ADTDyeSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTMortarBowlComponent, InteractUsingEvent>(OnBowlInteract);
        SubscribeLocalEvent<ADTMortarBowlComponent, ADTGrindDyeDoAfterEvent>(OnGrindFinished);
        SubscribeLocalEvent<ADTMortarComponent, AfterInteractEvent>(OnMortarInteract);
        SubscribeLocalEvent<ADTMortarComponent, ADTPaintDyeDoAfterEvent>(OnPaintFinished);
        SubscribeLocalEvent<ADTMortarComponent, ExaminedEvent>(OnMortarExamined);
        SubscribeLocalEvent<ADTRitualTotemComponent, ExaminedEvent>(OnTotemExamined);
    }

    private void OnBowlInteract(Entity<ADTMortarBowlComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !TryComp<ADTLavalandDyeComponent>(args.Used, out var dye))
            return;

        args.Handled = true;

        var doAfter = new DoAfterArgs(EntityManager, args.User, dye.GrindTime, new ADTGrindDyeDoAfterEvent(), ent.Owner, ent.Owner, args.Used)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            _popup.PopupEntity(Loc.GetString("adt-mortar-grind-start"), ent.Owner, args.User);
    }

    private void OnGrindFinished(Entity<ADTMortarBowlComponent> ent, ref ADTGrindDyeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used is not { } used)
            return;

        if (!TryComp<ADTLavalandDyeComponent>(used, out var dye))
            return;

        args.Handled = true;

        var mortar = Spawn(dye.Mortar, Transform(ent.Owner).Coordinates);

        QueueDel(used);
        QueueDel(ent.Owner);

        _hands.PickupOrDrop(args.User, mortar);
    }

    private void OnMortarInteract(Entity<ADTMortarComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target || target == args.User)
            return;

        var isTotem = HasComp<ADTRitualTotemComponent>(target);

        if (!isTotem && !IsValidTarget(ent, target))
            return;

        args.Handled = true;

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.Delay, new ADTPaintDyeDoAfterEvent(), ent.Owner, target, ent.Owner)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            _popup.PopupEntity(Loc.GetString("adt-mortar-paint-start", ("target", target)), ent.Owner, args.User);
    }

    private bool IsValidTarget(Entity<ADTMortarComponent> ent, EntityUid target)
    {
        if (!TryComp<HumanoidProfileComponent>(target, out var profile))
            return false;

        return ent.Comp.Species.Contains(profile.Species);
    }

    private void OnPaintFinished(Entity<ADTMortarComponent> ent, ref ADTPaintDyeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        args.Handled = true;

        var dyed = EnsureComp<ADTDyedComponent>(target);

        if (HasComp<ADTRitualTotemComponent>(target))
        {
            dyed.Dye = ent.Comp.Dye;
            dyed.Marking = null;
            Dirty(target, dyed);
            _popup.PopupEntity(Loc.GetString("adt-mortar-paint-totem", ("target", target)), target, args.User);
        }
        else
        {
            if (dyed.Marking is { } old)
                RemoveMarking(target, old);

            AddMarking(target, ent.Comp.Marking);

            dyed.Dye = ent.Comp.Dye;
            dyed.Marking = ent.Comp.Marking;
            Dirty(target, dyed);
            _popup.PopupEntity(Loc.GetString("adt-mortar-paint-body", ("target", target)), target, args.User);
        }

        ent.Comp.Uses--;

        if (ent.Comp.Uses > 0)
            return;

        var bowl = Spawn(ent.Comp.Empty, Transform(ent.Owner).Coordinates);

        QueueDel(ent.Owner);

        _hands.PickupOrDrop(args.User, bowl);
        _popup.PopupEntity(Loc.GetString("adt-mortar-empty"), bowl, args.User);
    }

    private void OnMortarExamined(Entity<ADTMortarComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("adt-mortar-examine-uses", ("uses", ent.Comp.Uses)));
    }

    private void OnTotemExamined(Entity<ADTRitualTotemComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("adt-totem-examine"));

        if (TryComp<ADTDyedComponent>(ent.Owner, out var dyed) && dyed.Dye != null)
            args.PushMarkup(Loc.GetString("adt-totem-examine-dye", ("dye", Loc.GetString($"adt-dye-name-{dyed.Dye.ToLowerInvariant()}"))));
    }

    public void AddMarking(EntityUid uid, ProtoId<MarkingPrototype> markingId)
    {
        if (!_proto.TryIndex(markingId, out var proto))
            return;

        if (!TryGetLayer(uid, proto.BodyPart, out var applied, out var markings))
            return;

        markings.RemoveAll(marking => marking.MarkingId == markingId);
        markings.Add(proto.AsMarking());

        _visualBody.ApplyMarkings(uid, applied);
    }

    public void RemoveMarking(EntityUid uid, ProtoId<MarkingPrototype> markingId)
    {
        if (!_proto.TryIndex(markingId, out var proto))
            return;

        if (!TryGetLayer(uid, proto.BodyPart, out var applied, out var markings))
            return;

        if (markings.RemoveAll(marking => marking.MarkingId == markingId) == 0)
            return;

        _visualBody.ApplyMarkings(uid, applied);
    }

    private bool TryGetLayer(
        EntityUid uid,
        HumanoidVisualLayers layer,
        out Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> applied,
        out List<Marking> markings)
    {
        applied = new();
        markings = new();

        if (!_visualBody.TryGatherMarkingsData(uid, null, out _, out var data, out var gathered))
            return false;

        applied = gathered;

        foreach (var (category, markingData) in data)
        {
            if (!markingData.Layers.Contains(layer) || !applied.TryGetValue(category, out var layers))
                continue;

            if (!layers.TryGetValue(layer, out var list))
            {
                list = new List<Marking>();
                layers[layer] = list;
            }

            markings = list;
            return true;
        }

        return false;
    }
}
