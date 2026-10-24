using Robust.Shared.GameObjects;

namespace Content.Shared.ADT.Shields;

/// <summary>Конденсатор рядом с генератором: каждый повышает лимит входа и резерв энергии.</summary>
[RegisterComponent]
public sealed partial class ShieldConduitComponent : Component
{
    public EntityUid? Generator;
}