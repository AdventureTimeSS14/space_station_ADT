using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Preferences;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling;

#region Actions
public sealed partial class LingAbsorbActionEvent : BaseTargetedChangelingActionEvent
{
}

public sealed partial class LingStingExtractActionEvent : BaseTargetedChangelingActionEvent
{
}

public sealed partial class BlindStingEvent : BaseTargetedChangelingActionEvent
{
}

public sealed partial class MuteStingEvent : BaseTargetedChangelingActionEvent
{
}

public sealed partial class DrugStingEvent : BaseTargetedChangelingActionEvent
{
}

public sealed partial class TransformationStingEvent : BaseTargetedChangelingActionEvent
{
}

public sealed partial class ChangelingEvolutionMenuActionEvent : BaseInstantChangelingActionEvent
{
}

public sealed partial class ChangelingTransformActionEvent : BaseInstantChangelingActionEvent
{
}

public sealed partial class LingRegenerateActionEvent : BaseInstantChangelingActionEvent
{
}

public sealed partial class ArmBladeActionEvent : BaseInstantChangelingActionEvent
{
}

public sealed partial class LingArmorActionEvent : BaseInstantChangelingActionEvent
{
}

public sealed partial class LingInvisibleActionEvent : BaseInstantChangelingActionEvent
{
}

public sealed partial class LingEMPActionEvent : BaseInstantChangelingActionEvent
{
}

public sealed partial class StasisDeathActionEvent : BaseInstantChangelingActionEvent
{
}

public sealed partial class AdrenalineActionEvent : BaseInstantChangelingActionEvent
{
}

public sealed partial class FleshmendActionEvent : BaseInstantChangelingActionEvent
{
}

public sealed partial class ChangelingRefreshActionEvent : BaseInstantChangelingActionEvent
{
}

public sealed partial class ChangelingMusclesActionEvent : BaseInstantChangelingActionEvent
{
}

public sealed partial class ChangelingLesserFormActionEvent : BaseInstantChangelingActionEvent
{
}

public sealed partial class ArmShieldActionEvent : BaseInstantChangelingActionEvent
{
}

public sealed partial class ArmaceActionEvent : BaseInstantChangelingActionEvent
{
}

public sealed partial class LastResortActionEvent : BaseInstantChangelingActionEvent
{
}

public sealed partial class LingBiodegradeActionEvent : BaseInstantChangelingActionEvent
{
}

public sealed partial class LingResonantShriekEvent : BaseInstantChangelingActionEvent
{
}

public sealed partial class DigitalCamouflageEvent : BaseInstantChangelingActionEvent
{
}

public sealed partial class ChangelingBoneShardEvent : BaseInstantChangelingActionEvent
{
}
#endregion

#region DoAfter
[Serializable, NetSerializable]
public sealed partial class AbsorbDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class BiodegradeDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class LingEggDoAfterEvent : SimpleDoAfterEvent
{
}
#endregion

#region Radial Menu
/// <summary>
/// This event carries humanoid information list of entities, which DNA were stolen. Used for radial UI of "The genestealer".
/// </summary>
[Serializable, NetSerializable]
public sealed partial class RequestChangelingFormsMenuEvent : EntityEventArgs
{
    public List<HDATA> HumanoidData = new();

    public NetEntity Target;
    public ChangelingMenuType Type;

    public RequestChangelingFormsMenuEvent(NetEntity target, ChangelingMenuType type)
    {
        Target = target;
        Type = type;
    }
}

[Serializable, NetSerializable]
public enum ChangelingMenuType : byte
{
    Transform,
    HumanForm,
    Sting,
}

[Serializable, NetSerializable]
public sealed class HDATA(NetEntity netEntity, string name, string species, HumanoidCharacterProfile profile)
{
    public NetEntity NetEntity = netEntity;
    public string Name = name;
    public string Species = species;
    public HumanoidCharacterProfile Profile = profile;
}


/// <summary>
/// This event carries prototype-id of emote, which was selected. This class is a part of code which is responsible for using RadialUiController.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SelectChangelingFormEvent : EntityEventArgs
{
    public NetEntity EntitySelected;

    public NetEntity User;
    public NetEntity Target;

    public bool Handled = false;
    public ChangelingMenuType Type;

    public SelectChangelingFormEvent(NetEntity user, NetEntity target, NetEntity entitySelected, ChangelingMenuType type)
    {
        User = user;
        Target = target;
        EntitySelected = entitySelected;
        Type = type;
    }
}
#endregion

#region Other
[NetSerializable, Serializable]
[DataDefinition]
public sealed partial class ChangelingRefreshEvent : EntityEventArgs
{
}

[DataDefinition]
public abstract partial class BaseTargetedChangelingActionEvent : EntityTargetActionEvent
{
    [DataField]
    public float Cost = 0f;
}

[DataDefinition]
public abstract partial class BaseInstantChangelingActionEvent : InstantActionEvent
{
    [DataField]
    public float Cost = 0f;
}
#endregion
