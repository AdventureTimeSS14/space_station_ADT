using Content.Shared.Examine;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.ADT.Clothing.Badge;

public sealed class BadgeSystem : EntitySystem
{
    [Dependency] protected readonly ExamineSystemShared ExamineSystem = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BadgeComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<BadgeComponent, ComponentStartup>(OnStartup);
    }

    private void OnExamined(EntityUid uid, BadgeComponent component, ExaminedEvent args)
    {
        if (!ExamineSystem.IsInDetailsRange(args.Examiner, uid))
        {
            if (component.NotInDetailsText != String.Empty)
                args.PushMarkup(Loc.GetString(component.NotInDetailsText, ("badgeNumber", component.BadgeNumber)));

            return;
        }

        if (component.InDetailsText != String.Empty)
            args.PushMarkup(Loc.GetString(component.InDetailsText, ("badgeNumber", component.BadgeNumber)));
    }

    private void OnStartup(EntityUid badge, BadgeComponent component, ComponentStartup args)
    {
        if (_net.IsClient)
            return;

        component.BadgeNumber = badge.Id + "-" + Random.Next(100, 99999) + "/" + Random.Next(1, 50);
        Dirty(badge, component);
    }
}
