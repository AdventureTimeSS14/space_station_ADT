using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Drake.Loot;

public sealed partial class ADTSacredFlameSpellEvent : InstantActionEvent
{
    [DataField]
    public float Range = 7f;

    [DataField]
    public float FireStacks = 20f;

    [DataField]
    public ComponentRegistry ResistComponents = new();
}
