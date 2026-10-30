using Content.Shared.ADT.AutoSurgeon;
using Content.Shared.ADT.Implants;
using Content.Shared.Body;
using Content.Shared.Buckle.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Popups;
using Robust.Server.Audio;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Server.ADT.AutoSurgeon;

public sealed class AutoSurgeonSystem : EntitySystem
{
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSubdermalImplantSystem _implants = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private EntityQuery<OrganComponent> _organQuery = default!;
    [Dependency] private EntityQuery<SubdermalImplantComponent> _implantQuery = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AutoSurgeonComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<AutoSurgeonComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<AutoSurgeonComponent, GetVerbsEvent<InteractionVerb>>(OnGetVerbs);
        SubscribeLocalEvent<AutoSurgeonComponent, DoAfterAttemptEvent<AutoSurgeonDoAfterEvent>>(OnDoAfterAttempt);
        SubscribeLocalEvent<AutoSurgeonComponent, AutoSurgeonDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<AutoSurgeonComponent, ExaminedEvent>(OnExamined);
    }

    private void OnInit(Entity<AutoSurgeonComponent> ent, ref ComponentInit args)
    {
        for (var i = 0; i < ent.Comp.ImplantSlots.Count; i++)
        {
            var slot = ent.Comp.ImplantSlots[i];
            if (i < ent.Comp.StartingItems.Count)
                slot.StartingItem = ent.Comp.StartingItems[i];

            _itemSlots.AddItemSlot(ent.Owner, $"{AutoSurgeonComponent.SlotId}_{i}", slot);
        }
    }

    private void OnRemove(Entity<AutoSurgeonComponent> ent, ref ComponentRemove args)
    {
        foreach (var slot in ent.Comp.ImplantSlots)
        {
            _itemSlots.RemoveItemSlot(ent.Owner, slot);
        }
    }

    private void OnGetVerbs(Entity<AutoSurgeonComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || ent.Comp.Running)
            return;

        var user = args.User;
        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("autosurgeon-verb-start"),
            Priority = 2,
            Act = () => StartOperation(ent, user),
        });
    }

    private void StartOperation(Entity<AutoSurgeonComponent> ent, EntityUid user)
    {
        if (ent.Comp.Running)
            return;

        if (GetStored(ent).Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("autosurgeon-empty"), ent, user);
            return;
        }

        if (ent.Comp.Uses == 0)
        {
            _popup.PopupEntity(Loc.GetString("autosurgeon-spent"), ent, user);
            return;
        }

        if (GetPatient(ent) is not { } patient)
        {
            _popup.PopupEntity(Loc.GetString("autosurgeon-no-patient"), ent, user);
            return;
        }

        if (ent.Comp.Steps.Count == 0)
            return;

        if (ent.Comp.CurrentPatient != patient || ent.Comp.CurrentStep >= ent.Comp.Steps.Count)
        {
            ent.Comp.CurrentStep = 0;
            ent.Comp.CurrentPatient = patient;
        }

        if (ent.Comp.CurrentStep == 0 && ent.Comp.StartSound != null)
            _audio.PlayPvs(ent.Comp.StartSound, ent);

        StartStep(ent, user, patient);
    }

    private void StartStep(Entity<AutoSurgeonComponent> ent, EntityUid user, EntityUid patient)
    {
        var step = ent.Comp.Steps[ent.Comp.CurrentStep];
        _popup.PopupEntity(Loc.GetString(step.Label), ent, user);

        var doAfter = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(step.Duration), new AutoSurgeonDoAfterEvent(), ent, patient, ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            ent.Comp.Running = true;
    }

    private EntityUid? GetPatient(Entity<AutoSurgeonComponent> ent)
    {
        if (!TryComp<StrapComponent>(ent, out var strap))
            return null;

        foreach (var buckled in strap.BuckledEntities)
        {
            if (HasComp<BodyComponent>(buckled))
                return buckled;
        }

        return null;
    }

    private void OnDoAfter(Entity<AutoSurgeonComponent> ent, ref AutoSurgeonDoAfterEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ent.Comp.Running = false;

        if (args.Cancelled || args.Target is not { } target || GetPatient(ent) != target)
        {
            _popup.PopupEntity(Loc.GetString("autosurgeon-interrupted"), ent, args.User, PopupType.SmallCaution);
            return;
        }

        var step = ent.Comp.Steps[ent.Comp.CurrentStep];

        if (step.Install && !TryInstall(ent, args.User, target))
        {
            return;
        }

        if (step.Damage.GetTotal() > 0)
            _damageable.TryChangeDamage(target, step.Damage, ignoreResistances: true);

        if (step.Sound != null)
            _audio.PlayPvs(step.Sound, ent);

        ent.Comp.CurrentStep++;
        if (ent.Comp.CurrentStep >= ent.Comp.Steps.Count)
        {
            ent.Comp.CurrentStep = 0;
            ent.Comp.CurrentPatient = null;
            _popup.PopupEntity(Loc.GetString("autosurgeon-done", ("patient", target)), ent, args.User, PopupType.Medium);
            return;
        }

        StartStep(ent, args.User, target);
    }

    private void OnDoAfterAttempt(Entity<AutoSurgeonComponent> ent, ref DoAfterAttemptEvent<AutoSurgeonDoAfterEvent> args)
    {
        if (args.DoAfter.Args.Target != GetPatient(ent))
            args.Cancel();
    }

    private void OnExamined(Entity<AutoSurgeonComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var stored = GetStored(ent);
        if (stored.Count == 0)
            args.PushMarkup(Loc.GetString("autosurgeon-examine-empty"));

        foreach (var item in stored)
        {
            args.PushMarkup(Loc.GetString("autosurgeon-examine-loaded", ("item", item)));
        }

        if (ent.Comp.Uses >= 0)
            args.PushMarkup(Loc.GetString("autosurgeon-examine-uses", ("uses", ent.Comp.Uses)));
    }

    private List<EntityUid> GetStored(Entity<AutoSurgeonComponent> ent)
    {
        var stored = new List<EntityUid>();
        foreach (var slot in ent.Comp.ImplantSlots)
        {
            if (slot.Item is { } item)
                stored.Add(item);
        }

        return stored;
    }

    private bool TryInstall(Entity<AutoSurgeonComponent> ent, EntityUid user, EntityUid target)
    {
        var stored = GetStored(ent);
        if (stored.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("autosurgeon-empty"), ent, user);
            return false;
        }

        if (ent.Comp.Uses == 0)
        {
            _popup.PopupEntity(Loc.GetString("autosurgeon-spent"), ent, user);
            return false;
        }

        if (!HasComp<BodyComponent>(target))
            return false;

        var installed = false;
        foreach (var item in stored)
        {
            if (Install(item, target))
            {
                installed = true;
                continue;
            }

            _popup.PopupEntity(Loc.GetString("autosurgeon-failed", ("item", item)), ent, user);
        }

        if (!installed)
            return false;

        if (ent.Comp.Uses > 0)
            ent.Comp.Uses--;

        Dirty(ent);
        return true;
    }

    private EntityUid? GetImplantHost(EntityUid item, EntityUid target)
    {
        if (!TryComp<OrganImplantComponent>(item, out var organImplant))
            return target;

        foreach (var organ in _body.EnumerateOrgans<OrganComponent>(target))
        {
            if (organ.Comp1.Category == organImplant.Organ)
                return organ.Owner;
        }

        return null;
    }

    private bool Install(EntityUid item, EntityUid target)
    {
        if (_implantQuery.TryComp(item, out var implant))
        {
            if (GetImplantHost(item, target) is not { } host)
                return false;

            if (Prototype(item) is { } proto && TryComp<ImplantedComponent>(host, out var implanted))
            {
                foreach (var existing in implanted.ImplantContainer.ContainedEntities)
                {
                    if (Prototype(existing) == proto)
                        return false;
                }
            }

            _implants.ForceImplant(host, (item, implant));
            return true;
        }

        if (!_organQuery.TryComp(item, out var organ))
            return false;

        foreach (var existing in _body.EnumerateOrgans<OrganComponent>(target))
        {
            if (existing.Comp1.Category == organ.Category)
                return false;
        }

        return _body.TryInsertOrgan(target, item);
    }
}
