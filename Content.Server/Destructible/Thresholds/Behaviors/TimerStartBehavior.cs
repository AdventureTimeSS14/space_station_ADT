/* (Откат PR - https://github.com/space-wizards/space-station-14/pull/32429)
namespace Content.Server.Destructible.Thresholds.Behaviors;

[DataDefinition]
public sealed partial class TimerStartBehavior : IThresholdBehavior
{
    public void Execute(EntityUid owner, DestructibleSystem system, EntityUid? cause = null)
    {
        system.TriggerSystem.StartTimer(owner, cause);
    }
}
Создает новый режим ограничения урона, 
который срабатывает для взрывчатых веществ и
заставляет их начать обратный отсчет. 
В сочетании с высокой устойчивостью к взрыву это позволяет сохранить бомбу,
которая не была взведена/сломана и отсчитывает время после срабатывания
от другого взрывчатого вещества.
*/