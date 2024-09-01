using Content.Shared.ADT.Phantom.Components;
using Content.Shared.ADT.Phantom;
using Content.Shared.Mobs;
using Content.Shared.Eye;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;

namespace Content.Server.ADT.Phantom.EntitySystems;

public sealed partial class PhantomVesselSystem : EntitySystem
{
    [Dependency] private readonly PhantomSystem _phantom = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VesselComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<VesselComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<VesselComponent, MobStateChangedEvent>(OnDeath);
        SubscribeLocalEvent<VesselComponent, EntityTerminatingEvent>(OnDeleted);
        SubscribeLocalEvent<VesselComponent, EctoplasmHitscanHitEvent>(OnEctoplasmicDamage);

        SubscribeLocalEvent<PhantomHolderComponent, MobStateChangedEvent>(OnHauntedDeath);
        SubscribeLocalEvent<PhantomHolderComponent, EntityTerminatingEvent>(OnHauntedDeleted);
        SubscribeLocalEvent<PhantomHolderComponent, EctoplasmHitscanHitEvent>(OnHauntedEctoplasmicDamage);
    }
    private void OnMapInit(EntityUid uid, VesselComponent component, MapInitEvent args)
    {
        if (!TryComp<EyeComponent>(uid, out var eyeComponent))
            return;
        _eye.SetVisibilityMask(uid, eyeComponent.VisibilityMask | (int) VisibilityFlags.PhantomVessel, eyeComponent);
    }
    private void OnShutdown(EntityUid uid, VesselComponent component, ComponentShutdown args)
    {
        if (!TryComp<PhantomComponent>(component.Phantom, out var phantom))
            return;

        phantom.Vessels.Remove(uid);
        var ev = new RefreshPhantomLevelEvent();
        RaiseLocalEvent(component.Phantom, ref ev);
        _phantom.PopulateVesselMenu(component.Phantom);

        if (phantom.Holder == uid)
            _phantom.StopHaunt(component.Phantom, uid, phantom);

        if (!TryComp<EyeComponent>(uid, out var eyeComponent))
            return;
        _eye.SetVisibilityMask(uid, eyeComponent.VisibilityMask & ~(int)VisibilityFlags.PhantomVessel, eyeComponent);
    }

    private void OnDeath(EntityUid uid, VesselComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            TryComp<PhantomComponent>(component.Phantom, out var comp);
            RemComp<VesselComponent>(uid);
            if (comp != null)
            {
                if (comp.TyranyStarted && comp.Vessels.Count <= 0)
                    _phantom.ChangeEssenceAmount(component.Phantom, -1000, allowDeath: true);
            }
        }
    }

    private void OnEctoplasmicDamage(EntityUid uid, VesselComponent component, EctoplasmHitscanHitEvent args)
    {
        _damageableSystem.TryChangeDamage(uid, args.DamageToTarget, true);
        _phantom.StopHaunt(component.Phantom, uid);
    }

    private void OnHauntedEctoplasmicDamage(EntityUid uid, PhantomHolderComponent component, EctoplasmHitscanHitEvent args)
    {
        _damageableSystem.TryChangeDamage(component.Phantom, args.DamageToPhantom);
        _damageableSystem.TryChangeDamage(uid, args.DamageToTarget, true);
        _phantom.StopHaunt(component.Phantom, uid);
    }

    private void OnHauntedDeath(EntityUid uid, PhantomHolderComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            if (TryComp<PhantomComponent>(component.Phantom, out var comp))
            {
                if (comp.TyranyStarted && comp.Vessels.Count <= 0)
                    _phantom.ChangeEssenceAmount(component.Phantom, -1000, allowDeath: true);
            }
        }
    }

    private void OnHauntedDeleted(EntityUid uid, PhantomHolderComponent component, EntityTerminatingEvent args)
    {
        if (!TryComp<PhantomComponent>(component.Phantom, out var phantom))
            return;
        _phantom.StopHaunt(component.Phantom, uid, phantom);
    }

    private void OnDeleted(EntityUid uid, VesselComponent component, EntityTerminatingEvent args)
    {
        if (!TryComp<PhantomComponent>(component.Phantom, out var phantom))
            return;

        phantom.Vessels.Remove(uid);
        var ev = new RefreshPhantomLevelEvent();
        RaiseLocalEvent(component.Phantom, ref ev);

        if (phantom.Holder == uid)
            _phantom.StopHaunt(component.Phantom, uid, phantom);

        if (!TryComp<EyeComponent>(uid, out var eyeComponent))
            return;
        _eye.SetVisibilityMask(uid, eyeComponent.VisibilityMask & ~(int) VisibilityFlags.PhantomVessel, eyeComponent);
    }
}
