using Content.Shared.ADT.Lavaland;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Systems;

namespace Content.Server.ADT.Lavaland;

public sealed class ADTNecropolisGateSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _faction = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTNecropolisGateComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ADTNecropolisGateComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<ADTNecropolisGateComponent, ADTNecropolisGateDoAfterEvent>(OnToggled);
        SubscribeLocalEvent<ADTNecropolisGateComponent, ExaminedEvent>(OnExamined);
    }

    private void OnMapInit(Entity<ADTNecropolisGateComponent> ent, ref MapInitEvent args)
    {
        Apply(ent);
    }

    private void OnInteractHand(Entity<ADTNecropolisGateComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (ent.Comp.Locked)
        {
            if (!CanUnlock(ent, args.User))
            {
                _popup.PopupPredicted(Loc.GetString("adt-necropolis-gate-locked"), ent.Owner, args.User);
                return;
            }

            ent.Comp.Locked = false;
            Dirty(ent);
            _popup.PopupPredicted(Loc.GetString("adt-necropolis-gate-unlocked"), ent.Owner, args.User);
        }

        var delay = ent.Comp.Open ? ent.Comp.CloseDelay : ent.Comp.OpenDelay;

        var doAfter = new DoAfterArgs(EntityManager, args.User, delay, new ADTNecropolisGateDoAfterEvent(), ent.Owner, ent.Owner)
        {
            BreakOnDamage = true,
            NeedHand = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        //_audio.PlayPredicted(ent.Comp.Sound, ent.Owner);
        _popup.PopupPredicted(
            Loc.GetString(ent.Comp.Open ? "adt-necropolis-gate-closing" : "adt-necropolis-gate-opening"),
            ent.Owner,
            args.User);
    }

    private void OnToggled(Entity<ADTNecropolisGateComponent> ent, ref ADTNecropolisGateDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        ent.Comp.Open = !ent.Comp.Open;
        Dirty(ent);
        Apply(ent);
    }

    private void OnExamined(Entity<ADTNecropolisGateComponent> ent, ref ExaminedEvent args)
    {
        if (ent.Comp.Locked)
            args.PushMarkup(Loc.GetString("adt-necropolis-gate-examine-locked"));
    }

    private bool CanUnlock(Entity<ADTNecropolisGateComponent> ent, EntityUid user)
    {
        if (ent.Comp.UnlockFaction is not { } faction)
            return true;

        return TryComp<NpcFactionMemberComponent>(user, out var member)
               && _faction.IsMember((user, member), faction);
    }

    private void Apply(Entity<ADTNecropolisGateComponent> ent)
    {
        _appearance.SetData(ent.Owner, ADTNecropolisGateVisuals.Open, ent.Comp.Open);
        _physics.SetCanCollide(ent.Owner, !ent.Comp.Open);
    }
}
