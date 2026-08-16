using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Holoparasite;

/// <summary>
/// Ментальная связь пары носитель-голопаразит.
/// </summary>
[RegisterComponent]
public sealed partial class HoloparasiteMindLinkComponent : Component
{
    [DataField]
    public EntityUid? Partner;
}
