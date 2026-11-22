using Content.Shared.Actions;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Drake.Loot;

public sealed partial class ADTLesserDrakeFireConeActionEvent : WorldTargetActionEvent;

public sealed partial class ADTLesserDrakeSwoopActionEvent : WorldTargetActionEvent
{
    [DataField]
    public TimeSpan Recovery = TimeSpan.FromSeconds(3);

    [DataField]
    public EntProtoId TargetMarker = "ADTLesserDrakeSwoopTarget";
}

public sealed partial class ADTDragonFormActionEvent : InstantActionEvent
{
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> Polymorph;

    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(5);

    [DataField]
    public ProtoId<EmotePrototype>? Emote = "Scream";

    [DataField]
    public ProtoId<DamageGroupPrototype> ConvertGroup = "Brute";
}

[Serializable, NetSerializable]
public sealed partial class ADTDragonFormDoAfterEvent : DoAfterEvent
{
    [DataField]
    public ProtoId<PolymorphPrototype> Polymorph;

    [DataField]
    public ProtoId<DamageGroupPrototype> ConvertGroup;

    private ADTDragonFormDoAfterEvent()
    {
    }

    public ADTDragonFormDoAfterEvent(ProtoId<PolymorphPrototype> polymorph, ProtoId<DamageGroupPrototype> convertGroup)
    {
        Polymorph = polymorph;
        ConvertGroup = convertGroup;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
