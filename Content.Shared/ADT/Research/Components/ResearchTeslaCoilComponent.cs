using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Research.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ResearchTeslaCoilComponent : Component
{
    /// <summary>
    /// Кол-во очков за попадание молнией
    /// </summary>
    [DataField]
    public int Points = 10;

    public bool HitByLightning = false;
}