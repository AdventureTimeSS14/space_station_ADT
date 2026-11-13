using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared.ADT.VoiceOfGod;

public abstract class SharedADTVoiceOfGodSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ADTVoiceOfGodComponent, ContainerGettingInsertedAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<ADTVoiceOfGodComponent, OrganGotInsertedEvent>(OnOrganInserted);
        SubscribeLocalEvent<ADTVoiceOfGodComponent, OrganGotRemovedEvent>(OnOrganRemoved);
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
        _popup.PopupEntity(Loc.GetString("adt-voice-of-god-insert-done"), args.Target, args.Target, PopupType.Medium);
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
