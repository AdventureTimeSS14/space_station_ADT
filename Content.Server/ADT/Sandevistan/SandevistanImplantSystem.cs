using Content.Shared.Implants;
using Content.Shared.ADT.Sandevistan;
using Content.Shared.Humanoid;
using Content.Server.Humanoid;
using Content.Shared.GrabProtection;
using Content.Shared.Body;
using Robust.Shared;

namespace Content.Server.ADT.Sandevistan;

public sealed class SandevistanImplantSystem : EntitySystem
{
    // коммент до почина
    // [UISystemDependency] private readonly VisualBodySystem _visualBody = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SandevistanImplantComponent, ImplantImplantedEvent>(OnImplanted);
        SubscribeLocalEvent<SandevistanImplantComponent, ImplantRemovedEvent>(OnRemoved);
    }

    private void OnImplanted(EntityUid uid, SandevistanImplantComponent comp, ref ImplantImplantedEvent args)
    {
        var owner = args.Implanted;

        EnsureComp<SandevistanUserComponent>(owner);
        EnsureComp<GrabProtectionComponent>(owner);

        // коммент до почина
        // if (!string.IsNullOrEmpty(comp.MarkingId) && TryComp<HumanoidProfileComponent>(owner, out var visualOrganMarkingsComponent))
        // {
        //     _visualBody.AddMarking(owner, comp.MarkingId, comp.MarkingColor, sync: true, forced: comp.ForcedMarking);
        // }
    }

    private void OnRemoved(EntityUid uid, SandevistanImplantComponent comp, ref ImplantRemovedEvent args)
    {
        var owner = args.Implanted;

        if (TryComp<SandevistanUserComponent>(owner, out var user))
        {
            RemComp<SandevistanUserComponent>(owner);
            RemComp<GrabProtectionComponent>(owner);

            // коммент до почина
            // if (!string.IsNullOrEmpty(comp.MarkingId) && TryComp<HumanoidProfileComponent>(owner, out var visualOrganMarkingsComponent))
            // {
            //     _visualBody.RemoveMarking(owner, comp.MarkingId, sync: true);
            // }
        }
    }
}



