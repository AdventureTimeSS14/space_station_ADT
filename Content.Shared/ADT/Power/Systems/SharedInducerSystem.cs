using Content.Shared.Examine;
using Content.Shared.Power.Components;

namespace Content.Shared.Power.EntitySystems;

public sealed class SharedInducerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InducerComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, InducerComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("inducer-examine-rate", ("rate", component.TransferRate)));
    }
}
