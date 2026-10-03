//

using Content.Shared.Heretic.Components.PathSpecific.Blade;
using Content.Shared.ADT.Heretic.Systems.PathSpecific.Blade;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.ADT.Heretic.EntitySystems.PathSpecific;

public sealed partial class SacramentsSystem : SharedSacramentsSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SacramentsOfPowerComponent, MapInitEvent>(OnMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;

        var query = EntityQueryEnumerator<SacramentsOfPowerComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out var comp, out var appearance))
        {
            if (comp.State == SacramentsState.Closing || time < comp.StateUpdateAt)
                continue;

            switch (comp.State)
            {
                case SacramentsState.Opening:
                    comp.StateUpdateAt = _timing.CurTime + comp.EffectTime;
                    _audio.PlayPvs(comp.Sound, uid);
                    comp.State = SacramentsState.Open;
                    Dirty(uid, comp);
                    break;
                case SacramentsState.Open:
                    comp.StateUpdateAt = _timing.CurTime + comp.DeactivationTime;
                    comp.State = SacramentsState.Closing;
                    Dirty(uid, comp);
                    break;
            }
        }
    }

    private void OnMapInit(Entity<SacramentsOfPowerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.StateUpdateAt = _timing.CurTime + ent.Comp.ActivationTime;
        EnsureComp<AppearanceComponent>(ent);
        _audio.PlayPvs(ent.Comp.ActivationSound, ent);
    }

    protected override void Pulse(EntityUid ent)
    {
        base.Pulse(ent);

        if (TryComp(ent, out SacramentsOfPowerComponent? comp))
            _audio.PlayPvs(comp.Sound, ent);
    }
}
