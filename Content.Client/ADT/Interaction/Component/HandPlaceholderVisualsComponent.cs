namespace Content.Shared.ADT.Interaction.Components;

[RegisterComponent]
// Клиентский компонент HandPlaceholder. Создает и отслеживает объект на стороне клиента для визуальных элементов, блокирующих руки.
public sealed partial class HandPlaceholderVisualsComponent : Component
{
    [DataField]
    public EntityUid Dummy;
}

