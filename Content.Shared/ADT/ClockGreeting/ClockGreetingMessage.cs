using Robust.Shared.Serialization;

namespace Content.Shared.ADT.ClockGreeting;

/// <summary>
/// Данные для приветствия при спавне: игровая дата, время и время смены.
/// </summary>
[Serializable, NetSerializable]
public sealed class ClockGreetingMessage(
    int year,
    int month,
    int day,
    int hour,
    int minute,
    int shiftHours,
    int shiftMinutes) : EntityEventArgs
{
    public int Year = year;
    public int Month = month;
    public int Day = day;
    public int Hour = hour;
    public int Minute = minute;
    public int ShiftHours = shiftHours;
    public int ShiftMinutes = shiftMinutes;
}
