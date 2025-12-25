using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Shizophrenia;

/// <summary>
/// Component added to hallucinating entity to prevent them from seeing mobs
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HallucinationsRemoveMobsComponent : Component
{
    [DataField]
    public string Reveal = "";

    [DataField]
    public string StartingMessage = "";
}
