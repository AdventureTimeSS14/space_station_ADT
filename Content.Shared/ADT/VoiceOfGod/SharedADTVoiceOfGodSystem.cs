using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared.ADT.VoiceOfGod;

public abstract class SharedADTVoiceOfGodSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTVoiceOfGodComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ADTVoiceOfGodComponent, ADTVoiceOfGodInsertDoAfterEvent>(OnInsertDoAfter);
        SubscribeLocalEvent<ADTVoiceOfGodComponent, ContainerGettingInsertedAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<ADTVoiceOfGodComponent, OrganGotInsertedEvent>(OnOrganInserted);
        SubscribeLocalEvent<ADTVoiceOfGodComponent, OrganGotRemovedEvent>(OnOrganRemoved);
    }

    private void OnUseInHand(Entity<ADTVoiceOfGodComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (!TryComp<BodyComponent>(args.User, out var body) || body.Organs == null)
        {
            _popup.PopupClient(Loc.GetString("adt-voice-of-god-no-body"), args.User, args.User);
            return;
        }

        if (HasOtherVoice(body.Organs, ent))
        {
            _popup.PopupClient(Loc.GetString("adt-voice-of-god-already-have"), args.User, args.User);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, args.User, ent.Comp.InsertDelay, new ADTVoiceOfGodInsertDoAfterEvent(), ent, args.User, ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupPredicted(
            Loc.GetString("adt-voice-of-god-insert-start-self", ("organ", ent.Owner)),
            Loc.GetString("adt-voice-of-god-insert-start-others", ("user", args.User), ("organ", ent.Owner)),
            args.User,
            args.User);
    }

    private void OnInsertDoAfter(Entity<ADTVoiceOfGodComponent> ent, ref ADTVoiceOfGodInsertDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        if (!TryComp<BodyComponent>(args.User, out var body) || body.Organs is not { } organs)
            return;

        if (!_container.Insert(ent.Owner, organs))
            return;

        _popup.PopupEntity(Loc.GetString("adt-voice-of-god-insert-done"), args.User, args.User, PopupType.Medium);
    }

    private void OnInsertAttempt(Entity<ADTVoiceOfGodComponent> ent, ref ContainerGettingInsertedAttemptEvent args)
    {
        if (args.Container.ID != BodyComponent.ContainerID)
            return;

        if (HasOtherVoice(args.Container, ent))
            args.Cancel();
    }

    private bool HasOtherVoice(BaseContainer organs, EntityUid organ)
    {
        foreach (var contained in organs.ContainedEntities)
        {
            if (contained != organ && HasComp<ADTVoiceOfGodComponent>(contained))
                return true;
        }

        return false;
    }

    private void OnOrganInserted(Entity<ADTVoiceOfGodComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (_net.IsClient)
            return;

        _actions.AddAction(args.Target, ref ent.Comp.ActionUid, ent.Comp.ActionProto, ent.Owner);
        EnsureComp<ADTVoiceOfGodSpeakerComponent>(args.Target).Organ = ent.Owner;
    }

    private void OnOrganRemoved(Entity<ADTVoiceOfGodComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (_net.IsClient)
            return;

        _actions.RemoveAction(ent.Comp.ActionUid);
        ent.Comp.ActionUid = null;
        ent.Comp.Active = false;

        if (TryComp<ADTVoiceOfGodSpeakerComponent>(args.Target, out var speaker) && speaker.Organ == ent.Owner)
            RemComp<ADTVoiceOfGodSpeakerComponent>(args.Target);
    }
}
