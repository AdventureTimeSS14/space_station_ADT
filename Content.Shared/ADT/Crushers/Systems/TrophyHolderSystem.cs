using Content.Shared.Administration.Logs;
using Content.Shared.ADT.Crushers.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Database;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Upgrades.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Crushers.Systems;

public sealed class TrophyHolderSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    [Dependency] private readonly TagSystem _tag = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<TrophyHolderComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<TrophyHolderComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<TrophyHolderComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<TrophyHolderComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<TrophyHolderComponent, EntRemovedFromContainerMessage>(OnEntRemoved);
    }

    private void OnInit(Entity<TrophyHolderComponent> ent, ref ComponentInit args)
    {
        _container.EnsureContainer<Container>(ent, ent.Comp.TrophyContainerId);
    }

    private void OnExamine(Entity<TrophyHolderComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(TrophyHolderComponent)))
        {
            foreach (var trophy in GetCurrentTrophies(ent))
            {
                args.PushMarkup(Loc.GetString(trophy.Comp.ExamineText));
            }
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
