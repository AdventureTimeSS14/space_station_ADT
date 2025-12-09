using Content.Shared.Actions;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Audio;

namespace Content.Shared._SD.Summonable;

public sealed partial class ContainerSummonActionEvent : EntityTargetActionEvent
{
    [DataField]
    public EntityTableSelector Table = default!;

    [DataField]
    public SoundSpecifier? SummonSound = new SoundPathSpecifier("/Audio/Items/toolbox_drop.ogg");

}
