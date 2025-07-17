using Content.Shared.Examine;
using Content.Server.SD.Cardreader.Components;
using Content.Shared.SD.Cardreader.Components;
using Robust.Shared.Localization;

namespace Content.Server.SD.Cardreader.Systems;

public sealed class RfidSystem : EntitySystem
{
    [Dependency] private readonly ILocalizationManager _loc = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RfidComponent, ExaminedEvent>(OnExamine);
    }
    private void OnExamine(EntityUid uid, RfidComponent component, ExaminedEvent args)
    {
        if (!component.LimitedUse)
            return;

        var message = Loc.GetString(
            component.LimitedUseExamineMessage,
            ("uses", component.UsesLeft),
            ("max", component.MaxUses)
        );

        args.PushMarkup(message);
    }


}



