using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Combat;

[RegisterComponent, NetworkedComponent]
public sealed partial class ComboComponent : Component
{
    [DataField]
    public List<CombatMove> AvailableMoves { get; private set; } = new List<CombatMove>();
    public EntityUid TargetEntity;
    public List<CombatAction> CurrestActions { get; private set; } = new List<CombatAction>();
    public EntityUid Target = default;

    /// <summary>
    /// делаю так, ибо через комбо это НЕРЕАЛЬНО сделать. Проще сделать переменную которя разрешает тому или иному комбо перелом шеи.
    /// </summary>
    [DataField("allowNeckSnap")]
    public bool AllowNeckSnap = false;
}
