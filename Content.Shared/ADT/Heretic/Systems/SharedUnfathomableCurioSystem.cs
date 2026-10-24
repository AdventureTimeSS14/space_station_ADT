//

using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Heretic.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.Heretic.Systems;

public sealed partial class SharedUnfathomableCurioSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<UnfathomableCurioShieldComponent>();
        while (query.MoveNext(out var uid, out var shield))
        {
            if (shield.Active)
                continue;

            if (now < shield.ActivateTime)
                continue;

            shield.Active = true;
            Dirty(uid, shield);
            _audio.PlayPvs(shield.RechargeSound, uid);
        }
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnfathomableCurioShieldComponent, BeforeHarmfulActionEvent>(OnBeforeHarmfulAction);
        SubscribeLocalEvent<UnfathomableCurioShieldComponent, BeforeDamageChangedEvent>(OnTakeDamage);
    }

    private void OnBeforeHarmfulAction(Entity<UnfathomableCurioShieldComponent> ent, ref BeforeHarmfulActionEvent args)
    {
        if (!ent.Comp.Active || args.Cancelled || args.Type != HarmfulActionType.Harm)
            return;

        args.Cancel();
        ResetShield(ent, true);
    }

    private void OnTakeDamage(Entity<UnfathomableCurioShieldComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Cancelled || args.Damage.GetTotal() < 5)
            return;

        if (!ent.Comp.Active)
        {
            ent.Comp.ActivateTime = _timing.CurTime + ent.Comp.ActivateDelay;
            Dirty(ent);
            return;
        }

        args.Cancelled = true;
        ResetShield(ent, true);
    }

    private void ResetShield(Entity<UnfathomableCurioShieldComponent> ent, bool playSound)
    {
        ent.Comp.Active = false;
        ent.Comp.ActivateTime = _timing.CurTime + ent.Comp.ActivateDelay;
        Dirty(ent);

        if (!playSound)
            return;

        _audio.PlayPredicted(ent.Comp.BlockSound, ent, null);
    }
}
