namespace Content.Shared.ADT.Addiction.AddictionComponent;
/// <summary>
/// Компонент для реагентов
/// 
/// </summary>
[RegisterComponent]
public sealed partial class AddictionComponent : Component 
{
    public float TimeCoefficient; // Коэфицент домножающий на себя время воздействие этого регаента на организм
    public float QuantityCoefficient; // Коэфицент домножающий на себя количество полученнного реагента организмом
}
