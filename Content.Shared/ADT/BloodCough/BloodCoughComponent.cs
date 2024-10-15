using Robust.Shared.GameStates;

namespace Content.Shared.ADT.BloodCough;

/// <summary>
/// Компонент, который отслеживает состояние сущности и воспроизводит эмоцию о плохом состоянии, если у сущности больше 70 грубого урона.
/// принимает поле postingSayDamage, то сообщение которое будет воспроиводиться эмоцией сущности.
/// by Шрёдька :з (Schrodinger71)
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class BloodCoughComponent : Component
{
    public TimeSpan NextSecond = TimeSpan.Zero;

    [DataField("coughTimeMin"), ViewVariables(VVAccess.ReadWrite)]
    public int CoughTimeMin = 11;

    [DataField("coughTimeMax"), ViewVariables(VVAccess.ReadWrite)]
    public int CoughTimeMax = 17;

    [DataField("postingSayDamage")]
    public string? PostingSayDamage = default;

    public bool CheckCoughBlood = false;
}
