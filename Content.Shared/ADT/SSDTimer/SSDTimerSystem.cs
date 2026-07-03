using Content.Shared.CCVar;
using Content.Shared.Examine;
using Content.Shared.SSDIndicator;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Shared.ADT.SSDTimer;

public sealed class SSDTimerSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

	private float _icSsdSleepTime;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SSDIndicatorComponent, ExaminedEvent>(OnExamined);

        _cfg.OnValueChanged(CCVars.ICSSDSleepTime, obj => _icSsdSleepTime = obj, true);

    }

    private void OnExamined(EntityUid uid, SSDIndicatorComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || !component.IsSSD)
            return;

		var timeFellAsleep = component.FallAsleepTime - TimeSpan.FromSeconds(_icSsdSleepTime);
		var time = _timing.CurTime - timeFellAsleep;
		args.PushMarkup(Loc.GetString("comp-ssd-person-examined", ("ent", uid), ("time", (int) time.TotalMinutes)));
    }
}
