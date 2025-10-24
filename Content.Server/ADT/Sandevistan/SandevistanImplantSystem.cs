using Content.Shared.Implants;
using Content.Shared.ADT.Sandevistan;

namespace Content.Server.ADT.Sandevistan;

public sealed class SandevistanImplantSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SandevistanImplantComponent, ImplantImplantedEvent>(OnImplanted);
        SubscribeLocalEvent<SandevistanImplantComponent, ImplantRemovedEvent>(OnRemoved);
    }

    private void OnImplanted(EntityUid uid, SandevistanImplantComponent comp, ref ImplantImplantedEvent args)
    {
        if (args.Implanted is not { } owner)
            return;

        EnsureComp<SandevistanUserComponent>(owner);
    }

    private void OnRemoved(EntityUid uid, SandevistanImplantComponent comp, ref ImplantRemovedEvent args)
    {
        if (args.Implanted is not { } owner)
            return;

        if (TryComp<SandevistanUserComponent>(owner, out var user))
        {
            RemComp<SandevistanUserComponent>(owner);
        }
    }
}



