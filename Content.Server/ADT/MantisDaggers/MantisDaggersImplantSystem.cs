using Content.Shared.Implants;
using Content.Shared.ADT.Implants;
using Content.Shared.ADT.MantisDaggers;

namespace Content.Server.ADT.MantisDaggers;

public sealed class MantisDaggersImplantSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MantisDaggersImplantComponent, ImplantImplantedEvent>(OnImplanted);
        SubscribeLocalEvent<MantisDaggersImplantComponent, ImplantRemovedEvent>(OnRemoved);
    }

    private void OnImplanted(EntityUid uid, MantisDaggersImplantComponent comp, ref ImplantImplantedEvent args)
    {
        if (args.Implanted is not { } owner)
            return;

        EnsureComp<MantisDaggersComponent>(owner);
    }

    private void OnRemoved(EntityUid uid, MantisDaggersImplantComponent comp, ref ImplantRemovedEvent args)
    {
        if (args.Implanted is not { } owner)
            return;

        if (TryComp<MantisDaggersComponent>(owner, out var user))
        {
            RemComp<MantisDaggersComponent>(owner);
        }
    }
}



