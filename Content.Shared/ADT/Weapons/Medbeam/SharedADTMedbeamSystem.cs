using Content.Shared.ADT.Damage.Components;
using Content.Shared.ADT.Heretic.Common;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Emp;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mech.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Weapons.Medbeam;

public abstract partial class SharedADTMedbeamSystem : EntitySystem
{
    [Dependency] protected readonly SharedContainerSystem Containers = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly MobStateSystem MobState = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public const string BeamId = "medbeam";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTMedbeamComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ADTMedbeamComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<ADTMedbeamComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<ADTMedbeamComponent, EntGotInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<ADTMedbeamComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<ADTMedbeamComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ADTMedbeamComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<ADTMedbeamComponent, ExaminedEvent>(OnExamine);
    }

    private void OnEmpPulse(Entity<ADTMedbeamComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        DetachBeam(ent);
    }

    private void OnAfterInteract(Entity<ADTMedbeamComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null)
            return;

        if (HasComp<EmpDisabledComponent>(ent.Owner))
        {
            args.Handled = true;
            return;
        }

        if (ent.Comp.RequireMech)
        {
            if (GetHolder(ent) is not { } holder || !HasComp<MechComponent>(holder))
                return;
        }

        DetachBeam(ent);

        if (args.Target is not { } target || target == args.User || !IsValidTarget(target))
        {
            args.Handled = true;
            return;
        }

        AttachBeam(ent, target);
        args.Handled = true;
    }

    protected virtual bool IsValidTarget(EntityUid target)
    {
        if (!HasComp<MobStateComponent>(target))
            return false;

        if (MobState.IsDead(target))
            return false;

        if (!TryComp<DamageableComponent>(target, out var damageable))
            return false;

        if (HasComp<MechComponent>(target) || HasComp<BorgChassisComponent>(target))
            return false;

        if (CompOrNull<InjurableComponent>(target)?.DamageContainer == ChangeDamageContainerComponent.BiologicalMetaphysicalContainer)
            return false;

        if (IsOnFire(target))
            return false;

        return true;
    }

    protected bool IsOnFire(EntityUid target)
    {
        return TryComp<FlammableComponent>(target, out var flammable)
            && (flammable.OnFire || flammable.FireStacks > 0);
    }

    protected ADTMedbeamMode? GetCurrentMode(Entity<ADTMedbeamComponent> ent)
    {
        if (ent.Comp.Modes.Count == 0)
            return null;

        var index = Math.Clamp(ent.Comp.CurrentModeIndex, 0, ent.Comp.Modes.Count - 1);
        return ent.Comp.Modes[index];
    }

    public virtual void AttachBeam(Entity<ADTMedbeamComponent> ent, EntityUid target)
    {
        if (HasComp<EmpDisabledComponent>(ent.Owner))
            return;

        ent.Comp.Target = target;
        Dirty(ent);

        var visuals = EnsureComp<ComplexJointVisualsComponent>(ent.Owner);
        visuals.Data[GetNetEntity(target)] =
            new ComplexJointVisualsData(BeamId, ent.Comp.Beam, ent.Comp.Start, ent.Comp.End, Timing.CurTime)
            {
                Scale = ent.Comp.Scale,
            };
        Dirty(ent.Owner, visuals);
    }

    public virtual void DetachBeam(Entity<ADTMedbeamComponent> ent)
    {
        if (ent.Comp.Target is not { } target)
            return;

        ent.Comp.Target = null;
        ent.Comp.Accumulator = 0;
        Dirty(ent);

        if (TryComp<ComplexJointVisualsComponent>(ent.Owner, out var visuals))
        {
            visuals.Data.Remove(GetNetEntity(target));
            if (visuals.Data.Count == 0)
                RemCompDeferred(ent.Owner, visuals);
            else
                Dirty(ent.Owner, visuals);
        }
    }

    protected EntityUid? GetHolder(Entity<ADTMedbeamComponent> ent)
    {
        if (Containers.TryGetContainingContainer((ent.Owner, null), out var container))
            return container.Owner;

        return null;
    }

    private void OnActivate(Entity<ADTMedbeamComponent> ent, ref ActivateInWorldEvent args)
    {
        if (!args.Complex || args.Handled)
            return;

        DetachBeam(ent);
        args.Handled = true;
    }

    private void OnDropped(Entity<ADTMedbeamComponent> ent, ref DroppedEvent args)
    {
        DetachBeam(ent);
    }

    private void OnInserted(Entity<ADTMedbeamComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        DetachBeam(ent);
    }

    private void OnUseInHand(Entity<ADTMedbeamComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || ent.Comp.Modes.Count == 0)
            return;

        if (HasComp<EmpDisabledComponent>(ent.Owner))
        {
            args.Handled = true;
            return;
        }

        TrySetMode(ent, (ent.Comp.CurrentModeIndex + 1) % ent.Comp.Modes.Count, args.User);
        args.Handled = true;
    }

    private void OnGetVerbs(Entity<ADTMedbeamComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract)
            return;

        if (ent.Comp.Modes.Count < 2)
            return;

        var user = args.User;
        for (var i = 0; i < ent.Comp.Modes.Count; i++)
        {
            var mode = ent.Comp.Modes[i];
            var index = i;

            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.SelectType,
                Text = Loc.GetString(mode.Name),
                Disabled = i == ent.Comp.CurrentModeIndex,
                Impact = LogImpact.Medium,
                DoContactInteraction = true,
                Act = () => TrySetMode((ent.Owner, ent.Comp), index, user),
            };

            args.Verbs.Add(v);
        }
    }

    private void TrySetMode(Entity<ADTMedbeamComponent> ent, int index, EntityUid? user = null)
    {
        if (index < 0 || index >= ent.Comp.Modes.Count || ent.Comp.CurrentModeIndex == index)
            return;

        if (Timing.CurTime < ent.Comp.NextModeSwitchTime)
        {
            _popup.PopupClient(Loc.GetString("medbeam-mode-cooldown"), ent.Owner, user);
            return;
        }

        ent.Comp.CurrentModeIndex = index;
        ent.Comp.NextModeSwitchTime = Timing.CurTime + ent.Comp.ModeSwitchCooldown;
        Dirty(ent);

        DetachBeam(ent);

        var mode = ent.Comp.Modes[index];
        _popup.PopupClient(Loc.GetString("medbeam-mode-changed", ("mode", Loc.GetString(mode.Name))), ent.Owner, user);
        _audio.PlayPredicted(ent.Comp.ModeSwitchSound, ent.Owner, user);
    }

    private void OnExamine(Entity<ADTMedbeamComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || GetCurrentMode(ent) is not { } mode)
            return;

        args.PushMarkup(Loc.GetString("medbeam-mode-examine", ("mode", Loc.GetString(mode.Name))));
    }
}