using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Traits.Assorted;

/// <summary>
/// Set player speed to zero and standing state to down, simulating leg paralysis.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(LegsParalyzedCrawlingSystem))]
public sealed partial class LegsParalyzedCrawlyngComponent : Component
{
    public bool AddedKnockdown;
}
