using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Body.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.Events;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.ADT.NPC;

public sealed partial class ADTNpcBreathingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private AtmosphereSystem _atmos = default!;
    [Dependency] private GasTankSystem _gasTank = default!;
    [Dependency] private InternalsSystem _internals = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ADTNpcBreathingComponent, InternalsComponent, RespiratorComponent>();
        while (query.MoveNext(out var uid, out var breathing, out var internals, out var respirator))
        {
            if (now < breathing.NextCheck)
                continue;

            breathing.NextCheck = now + breathing.CheckInterval;

            if (!HasComp<ActiveNPCComponent>(uid) || HasComp<ActorComponent>(uid) || _mobState.IsDead(uid))
                continue;

            if (!_inventory.TryGetSlotEntity(uid, "mask", out var mask) || !HasComp<BreathToolComponent>(mask))
                continue;

            if (!TryClassifyGases(uid, respirator, breathing, now))
                continue;

            TryComp<MaskComponent>(mask, out var maskComp);

            if (!IsBreathable(_atmos.GetContainingMixture(uid), breathing))
            {
                breathing.SafeSince = null;

                if (maskComp is { IsToggled: true })
                {
                    if (maskComp.IsToggleable)
                    {
                        SetMaskDown(uid, mask.Value, false);
                        breathing.NextCheck = now + breathing.MaskRaiseRecheck;
                    }

                    continue;
                }

                if (_internals.AreInternalsWorking(uid, internals))
                    continue;

                if (_internals.FindBestGasTank(uid) is { } tank && IsBreathable(tank.Comp.Air, breathing))
                    _gasTank.ConnectToInternals(tank, uid);

                continue;
            }

            breathing.SafeSince ??= now;
            if (now - breathing.SafeSince.Value < breathing.SafeDelay)
                continue;

            if (maskComp is { IsToggled: false, IsToggleable: true })
            {
                SetMaskDown(uid, mask.Value, true);
                continue;
            }

            if (internals.GasTankEntity is { } tankUid && TryComp<GasTankComponent>(tankUid, out var tankComp))
                _gasTank.DisconnectFromInternals((tankUid, tankComp), uid);
        }
    }

    private bool TryClassifyGases(EntityUid uid, RespiratorComponent respirator, ADTNpcBreathingComponent breathing, TimeSpan now)
    {
        if (breathing.BreathableGases != null && breathing.ToxicGases != null && now < breathing.ClassifiedUntil)
            return true;

        var breathable = new bool[Atmospherics.TotalNumberOfGases];
        var toxic = new bool[Atmospherics.TotalNumberOfGases];

        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            var test = new GasMixture(respirator.BreathVolume) { Temperature = Atmospherics.T20C };
            test.SetMoles(i, 1f);

            var ev = new CanMetabolizeGasEvent(test);
            RaiseLocalEvent(uid, ref ev);

            if (!ev.Handled)
                return false;

            toxic[i] = ev.Toxic;
            breathable[i] = !ev.Toxic && ev.Saturation > 0f;
        }

        breathing.BreathableGases = breathable;
        breathing.ToxicGases = toxic;
        breathing.ClassifiedUntil = now + breathing.ClassifyInterval;
        return true;
    }

    private bool IsBreathable(GasMixture? air, ADTNpcBreathingComponent breathing)
    {
        if (air == null || breathing.BreathableGases == null || breathing.ToxicGases == null)
            return false;

        var total = air.TotalMoles;
        if (total <= 0f)
            return false;

        var pressure = air.Pressure;
        var breathablePressure = 0f;

        for (var i = 0; i < Atmospherics.TotalNumberOfGases; i++)
        {
            var moles = air.GetMoles(i);
            if (moles <= 0f)
                continue;

            var partial = pressure * moles / total;

            if (breathing.ToxicGases[i] && partial > breathing.ToxicPressure)
                return false;

            if (breathing.BreathableGases[i])
                breathablePressure += partial;
        }

        return breathablePressure >= breathing.MinBreathablePressure;
    }

    private void SetMaskDown(EntityUid owner, EntityUid mask, bool down)
    {
        var args = new DoAfterArgs(EntityManager, owner, TimeSpan.Zero, new ToggleMaskDoAfterEvent(down, false, false), mask, target: owner);
        _doAfter.TryStartDoAfter(args);
    }
}
