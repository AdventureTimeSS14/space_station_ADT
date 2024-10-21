
namespace Content.Shared.ADT.Clothing.Badge;

[RegisterComponent]
public sealed partial class BadgeableComponent : Component
{
    public string Slot = "badge";
    public string NotInDetailsText = "badgeable-badge-cannot-be-seen-text";
    public string InDetailsText = "badgeable-badge-can-be-seen-text";
}
