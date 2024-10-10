using System.Diagnostics.CodeAnalysis;
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
    }

    private void OnExamined(EntityUid uid, BadgeComponent component, ExaminedEvent args)
    {
        if (!GetBadgeNumber(uid, out var badgeNumber, component))
            return;

        if (!ExamineSystem.IsInDetailsRange(args.Examiner, uid))
        {
            if (component.NotInDetailsText != String.Empty)
                args.PushMarkup(Loc.GetString(component.NotInDetailsText, ("badgeNumber", badgeNumber)));

            return;
        }

        if (component.InDetailsText != String.Empty)
            args.PushMarkup(Loc.GetString(component.InDetailsText, ("badgeNumber", badgeNumber)));
    }

    public bool GetBadgeNumber(EntityUid badge, [NotNullWhen(true)] out string? badgeNumber, BadgeComponent? component = null)
    {
        badgeNumber = null;
        if (!Resolve(badge, ref component))
            return false;

        if (component.BadgeNumber == String.Empty)
        {
            if (_net.IsClient)
                return false;
            component.BadgeNumber = GenerateBadgeNumber(badge, component);
            Dirty(badge, component);
        }

        badgeNumber = component.BadgeNumber;
        return true;
    }

    private string GenerateBadgeNumber(EntityUid badge, BadgeComponent component)
    {
        return badge.Id + "-" + Random.Next(100, 99999) + "/" + Random.Next(1, 50);
    }
}
