using Content.Shared.ADT.Chaplain.Components;
using Content.Shared.Bible.Components;
using Content.Shared.Damage;
using Content.Shared.Hands;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Jittering;
using Content.Shared.Speech.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.StatusEffect;
using Content.Shared.Movement.Systems;
using Content.Shared.Item;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Electrocution;

namespace Content.Server.ADT.Chaplain.Systems;

public sealed class ForTheWorthySystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;
    [Dependency] private readonly SharedStutteringSystem _stuttering = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ForTheWorthyComponent, GettingPickedUpAttemptEvent>(OnPickupAttempt);
    }

    private void OnPickupAttempt(EntityUid uid, ForTheWorthyComponent component, GettingPickedUpAttemptEvent args)
    {
        if (HasComp<ChaplainComponent>(args.User))
            return;

        args.Cancel();
        _popup.PopupEntity(Loc.GetString("chaplain-for-the-worthy"), args.User, args.User);

        ApplyCustomElectrocution(args.User, uid, component);

        _audio.PlayPvs(component.ElectrocutionSound, args.User);
    }

    private void ApplyCustomElectrocution(EntityUid target, EntityUid source, ForTheWorthyComponent component)
    {
        var damageSpec = new DamageSpecifier();
        if (_prototype.TryIndex<DamageTypePrototype>("Heat", out var shockDamageType))
        {
            damageSpec.DamageDict.Add(shockDamageType.ID, 2);
        }

        _damageable.TryChangeDamage(target, damageSpec, origin: source);

        _stun.TryParalyze(target, TimeSpan.FromSeconds(2), true);

        _jittering.DoJitter(target, TimeSpan.FromSeconds(2), true, 80, 8);

        _stuttering.DoStutter(target, TimeSpan.FromSeconds(3), true);
    }
}