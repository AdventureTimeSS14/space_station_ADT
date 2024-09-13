using Content.Shared.ADT.NotLungAlertOxygen;
using Content.Server.Body.Components;
using Content.Shared.Alert;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Server.Body.Systems;

namespace Content.Server.ADT.NotLungAlertOxygen;
public sealed class NotLungAlertOxygenSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly BodySystem _bodySystem = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RespiratorComponent, NotLungAlertOxygenComponent>();
        while (query.MoveNext(out var uid, out var respirator, out var _))
        {
            if (_gameTiming.CurTime < respirator.NextUpdate)
                continue;

            respirator.NextUpdate += respirator.UpdateInterval;

            if (_mobState.IsDead(uid))
                continue;

            if (respirator.Saturation < respirator.SuffocationThreshold)
            {
                // Самая важная часть, тут мы чистим алерт кислородного голомадния, чтобы не показывалось
                // TODO: This is not going work with multiple different lungs, if that ever becomes a possibility
                var organs = _bodySystem.GetBodyOrganEntityComps<LungComponent>((uid, null));
                foreach (var entity in organs)
                {
                    _alertsSystem.ClearAlert(uid, entity.Comp1.Alert);
                }
                continue;
            }
        }
    }
}

