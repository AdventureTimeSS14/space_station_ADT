using Content.Server.ADT.Silicons.Borgs.Components;
using Content.Server.Silicons.Laws;
using Content.Shared.Mind.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;
using Robust.Server.GameObjects;

namespace Content.Server.ADT.Silicons.Borgs;

public sealed class ADTBorgBrainLawsSystem : EntitySystem
{
    [Dependency] private readonly SiliconLawSystem _laws = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgChassisComponent, GetSiliconLawsEvent>(OnChassisGetLaws);
        SubscribeLocalEvent<ADTBorgAiLinkComponent, MindAddedMessage>(OnChassisMindAdded);
    }

    private void OnChassisGetLaws(Entity<BorgChassisComponent> ent, ref GetSiliconLawsEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<SiliconLawProviderComponent>(ent))
            return;

        if (ent.Comp.BrainEntity is not { } brain || !HasComp<SiliconLawProviderComponent>(brain))
            return;

        args.Laws = _laws.GetLaws(brain);
        args.Handled = true;
    }

    private void OnChassisMindAdded(Entity<ADTBorgAiLinkComponent> ent, ref MindAddedMessage args)
    {
        if (!TryComp<BorgChassisComponent>(ent, out var chassis))
            return;

        if (chassis.BrainEntity is not { } brain
            || !TryComp<SiliconLawProviderComponent>(brain, out var provider)
            || !provider.Subverted)
            return;

        if (!HasComp<SiliconLawBoundComponent>(ent))
            return;

        _laws.NotifyHostBorgSubverted(brain);
    }
}
