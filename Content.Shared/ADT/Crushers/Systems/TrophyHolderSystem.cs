using Content.Shared.Administration.Logs;
using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Crushers.Systems;

public sealed class TrophyHolderSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TrophyHolderComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<TrophyHolderComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<TrophyHolderComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<TrophyHolderComponent, GetVerbsEvent<ExamineVerb>>(OnTrophyVerbExamine);

        SubscribeLocalEvent<TrophyHolderComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<TrophyHolderComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
    }

    private void OnInit(Entity<TrophyHolderComponent> ent, ref ComponentInit args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.TrophyContainerId);
    }

    private void OnTrophyVerbExamine(Entity<TrophyHolderComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var trophies = GetCurrentTrophies(ent);
        if (trophies.Count == 0)
            return;

        var examineMarkup = new FormattedMessage();

        foreach (var trophy in trophies)
        {
            foreach (var effect in trophy.Comp.Effects)
            {
                var description = effect.GetDescription();
                examineMarkup.AddMessage(description);
                examineMarkup.PushNewline();
                examineMarkup.PushNewline();
            }
        }

        _examine.AddDetailedExamineVerb(args, ent.Comp, examineMarkup,
            Loc.GetString("trophy-examinable-verb-text"), "/Textures/Interface/VerbIcons/dot.svg.192dpi.png",
            Loc.GetString("trophy-examinable-verb-message"));
    }

    private void OnInteractUsing(Entity<TrophyHolderComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (_tool.HasQuality(args.Used, ent.Comp.ToolQuality))
        {
            if (!_container.TryGetContainer(ent, ent.Comp.TrophyContainerId, out var container) || container.ContainedEntities.Count == 0)
                return;

            var lastTrophy = container.ContainedEntities[^1];

            if (!_container.Remove(lastTrophy, container))
                return;

            _transform.SetCoordinates(lastTrophy, _transform.GetMoverCoordinates(args.User));
            _hands.TryPickupAnyHand(args.User, lastTrophy);

            _audio.PlayPredicted(new SoundPathSpecifier("/Audio/Items/crowbar.ogg"), ent, args.User);
            _popup.PopupClient(Loc.GetString("crusher-upgrade-popup-extracted", ("upgrade", lastTrophy), ("crusher", ent.Owner)), args.User);

            _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):player} extracted crusher upgrade {ToPrettyString(lastTrophy)} from {ToPrettyString(ent.Owner)}.");

            args.Handled = true;
        }
    }

    private void OnAfterInteractUsing(Entity<TrophyHolderComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || !args.CanReach)
            return;

        if (!TryComp<TrophyComponent>(args.Used, out var trophyComp))
            return;

        if (!CanInsert(ent, (args.Used, trophyComp), out var reason))
        {
            _popup.PopupClient(Loc.GetString(reason), args.User);
            return;
        }

        if (!_container.Insert(args.Used, _container.GetContainer(ent, ent.Comp.TrophyContainerId)))
            return;

        args.Handled = true;

        _audio.PlayPredicted(ent.Comp.InsertSound, ent, args.User);
        _popup.PopupClient(Loc.GetString("crusher-upgrade-popup-insert", ("upgrade", args.Used), ("crusher", ent.Owner)), args.User);
        _gun.RefreshModifiers(ent.Owner);

        _adminLog.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(args.User):player} inserted crusher upgrade {ToPrettyString(args.Used)} into {ToPrettyString(ent.Owner)}.");
    }

    private bool CanInsert(Entity<TrophyHolderComponent> ent, Entity<TrophyComponent> used, out LocId reason)
    {
        reason = default;

        foreach (var trophy in GetCurrentTrophies(ent))
        {
            if (_tag.HasTag(trophy, used.Comp.TrophyTag))
            {
                reason = "crusher-upgrade-popup-already-present";
                return false;
            }
        }

        return true;
    }

    public HashSet<Entity<TrophyComponent>> GetCurrentTrophies(Entity<TrophyHolderComponent> ent)
    {
        if (!_container.TryGetContainer(ent, ent.Comp.TrophyContainerId, out var container))
            return [];

        var trophies = new HashSet<Entity<TrophyComponent>>();
        foreach (var contained in container.ContainedEntities)
        {
            if (TryComp<TrophyComponent>(contained, out var trophyComp))
                trophies.Add((contained, trophyComp));
        }

        return trophies;
    }

    private void OnEntInserted(Entity<TrophyHolderComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.TrophyContainerId)
            return;

        if (!HasComp<TrophyComponent>(args.Entity))
            return;

        var ev = new TrophyAlteredEvent(ent.Owner, TrophyAlteredType.Inserted);
        RaiseLocalEvent(args.Entity, ref ev);

        var holderEv = new TrophyHolderTrophiesAlteredEvent(args.Entity, TrophyAlteredType.Inserted);
        RaiseLocalEvent(ent.Owner, ref holderEv);

        _gun.RefreshModifiers(ent.Owner);
    }

    private void OnEntRemoved(Entity<TrophyHolderComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.TrophyContainerId)
            return;

        if (!HasComp<TrophyComponent>(args.Entity))
            return;

        var ev = new TrophyAlteredEvent(ent.Owner, TrophyAlteredType.Removed);
        RaiseLocalEvent(args.Entity, ref ev);

        var holderEv = new TrophyHolderTrophiesAlteredEvent(args.Entity, TrophyAlteredType.Removed);
        RaiseLocalEvent(ent.Owner, ref holderEv);

        _gun.RefreshModifiers(ent.Owner);
    }
}
