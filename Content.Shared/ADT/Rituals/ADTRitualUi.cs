using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Rituals;

[Serializable, NetSerializable]
public enum ADTRitualUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public struct ADTRitualEntry
{
    public ProtoId<ADTRitualPrototype> Ritual;

    public int Charges;

    public TimeSpan CooldownEnd;

    public ADTRitualEntry(ProtoId<ADTRitualPrototype> ritual, int charges, TimeSpan cooldownEnd)
    {
        Ritual = ritual;
        Charges = charges;
        CooldownEnd = cooldownEnd;
    }

    public readonly int CooldownLeft(TimeSpan now)
    {
        var left = CooldownEnd - now;
        return left <= TimeSpan.Zero ? 0 : (int)Math.Ceiling(left.TotalSeconds);
    }

    public readonly bool Available(TimeSpan now) => Charges != 0 && CooldownLeft(now) <= 0;
}

[Serializable, NetSerializable]
public sealed class ADTRitualBuiState : BoundUserInterfaceState
{
    public List<ADTRitualEntry> Rituals;

    public bool Busy;

    public ADTRitualBuiState(List<ADTRitualEntry> rituals, bool busy)
    {
        Rituals = rituals;
        Busy = busy;
    }
}

[Serializable, NetSerializable]
public sealed class ADTRitualStartMessage : BoundUserInterfaceMessage
{
    public ProtoId<ADTRitualPrototype> Ritual;

    public ADTRitualStartMessage(ProtoId<ADTRitualPrototype> ritual)
    {
        Ritual = ritual;
    }
}
