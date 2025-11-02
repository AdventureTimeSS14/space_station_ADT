using Content.Shared.Implants;
using Content.Shared.ADT.Sandevistan;
using Content.Shared.Humanoid;
using Content.Server.Humanoid;
using Content.Shared.GrabProtection;

namespace Content.Server.ADT.Sandevistan;

public sealed class SandevistanImplantSystem : EntitySystem
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidSystem = default!;

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
        EnsureComp<GrabProtectionComponent>(owner);

        if (!string.IsNullOrEmpty(comp.MarkingId) && TryComp<HumanoidAppearanceComponent>(owner, out _))
        {
            _humanoidSystem.AddMarking(owner, comp.MarkingId, comp.MarkingColor, sync: true, forced: comp.ForcedMarking);
        }
    }

    private void OnRemoved(EntityUid uid, SandevistanImplantComponent comp, ref ImplantRemovedEvent args)
    {
        if (args.Implanted is not { } owner)
            return;

        if (TryComp<SandevistanUserComponent>(owner, out var user))
        {
            RemComp<SandevistanUserComponent>(owner);
            RemComp<GrabProtectionComponent>(owner);
            if (!string.IsNullOrEmpty(comp.MarkingId) && TryComp<HumanoidAppearanceComponent>(owner, out _))
            {
                _humanoidSystem.RemoveMarking(owner, comp.MarkingId, sync: true);
            }
        }
    }
}



