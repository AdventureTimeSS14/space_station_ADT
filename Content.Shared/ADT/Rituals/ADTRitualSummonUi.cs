using Content.Shared.ADT.UI;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Rituals;

[Serializable, NetSerializable]
public enum ADTRitualSummonUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ADTRitualSummonBuiState : BoundUserInterfaceState
{
    public List<ADTEntityPickerEntry> Candidates;

    public ADTRitualSummonBuiState(List<ADTEntityPickerEntry> candidates)
    {
        Candidates = candidates;
    }
}

[Serializable, NetSerializable]
public sealed class ADTRitualSummonSelectMessage : BoundUserInterfaceMessage
{
    public NetEntity Target;

    public ADTRitualSummonSelectMessage(NetEntity target)
    {
        Target = target;
    }
}
