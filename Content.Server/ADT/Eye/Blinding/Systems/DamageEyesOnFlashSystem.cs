using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Eye.Blinding;

public sealed class DamageEyesOnFlashSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageEyesOnFlashedComponent, FlashedEvent>(OnFlashed);
    }

    private void OnFlashed(EntityUid uid, DamageEyesOnFlashedComponent comp, ref FlashedEvent args)
    {
        if (HasComp<NoEyeDamageOnFlashComponent>(args.Used))
            return;
        if (_timing.CurTime < comp.NextDamage)
            return;

        _blindable.AdjustEyeDamage(uid, comp.FlashDamage);
        comp.NextDamage = _timing.CurTime + TimeSpan.FromSeconds(3);
    }
}
