using Robust.Shared.Serialization;

namespace Content.Shared.ADT.ClockGreeting;

/// <summary>
/// Данные для приветствия при спавне: игровая дата и время смены.
/// </summary>
[Serializable, NetSerializable]
public sealed class ClockGreetingMessage(DateTime date, TimeSpan shift) : EntityEventArgs
{
    public DateTime Date = date;
    public TimeSpan Shift = shift;
}
