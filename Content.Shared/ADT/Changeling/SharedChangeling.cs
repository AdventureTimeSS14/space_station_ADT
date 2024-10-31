using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Preferences;
using Robust.Shared.Serialization;

namespace Content.Shared.Changeling;

public sealed partial class LingAbsorbActionEvent : EntityTargetActionEvent
{
}

public sealed partial class LingStingExtractActionEvent : EntityTargetActionEvent
{
}

public sealed partial class BlindStingEvent : EntityTargetActionEvent
{
}

public sealed partial class MuteStingEvent : EntityTargetActionEvent
{
}

public sealed partial class DrugStingEvent : EntityTargetActionEvent
{
}

public sealed partial class TransformationStingEvent : EntityTargetActionEvent
{
}

public sealed partial class LingEggActionEvent : EntityTargetActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class AbsorbDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class LingEggDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class BiodegradeDoAfterEvent : SimpleDoAfterEvent
{
}
public sealed partial class ChangelingEvolutionMenuActionEvent : InstantActionEvent
{
}

public sealed partial class ChangelingCycleDNAActionEvent : InstantActionEvent
{
}

public sealed partial class ChangelingTransformActionEvent : InstantActionEvent
{
}

public sealed partial class LingRegenerateActionEvent : InstantActionEvent
{
}

public sealed partial class ArmBladeActionEvent : InstantActionEvent
{
}

public sealed partial class LingArmorActionEvent : InstantActionEvent
{
}

public sealed partial class LingInvisibleActionEvent : InstantActionEvent
{
}

public sealed partial class LingEMPActionEvent : InstantActionEvent
{
}

public sealed partial class StasisDeathActionEvent : InstantActionEvent
{
}

public sealed partial class AdrenalineActionEvent : InstantActionEvent
{
}

public sealed partial class FleshmendActionEvent : InstantActionEvent
{
}

public sealed partial class ChangelingRefreshActionEvent : InstantActionEvent
{
}

public sealed partial class ChangelingMusclesActionEvent : InstantActionEvent
{
}

public sealed partial class ChangelingLesserFormActionEvent : InstantActionEvent
{
}

public sealed partial class ArmShieldActionEvent : InstantActionEvent
{
}

public sealed partial class ArmaceActionEvent : InstantActionEvent
{
}

public sealed partial class LastResortActionEvent : InstantActionEvent
{
}

public sealed partial class LingEggSpawnActionEvent : InstantActionEvent
{
}

public sealed partial class LingHatchActionEvent : InstantActionEvent
{
}
public sealed partial class LingBiodegradeActionEvent : InstantActionEvent
{
}

public sealed partial class LingResonantShriekEvent : InstantActionEvent
{
}

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

    public NetEntity Target;

    public bool Handled = false;
    public ChangelingMenuType Type;

    public SelectChangelingFormEvent(NetEntity target, NetEntity entitySelected, ChangelingMenuType type)
    {
        Target = target;
        EntitySelected = entitySelected;
        Type = type;
    }
}

[NetSerializable, Serializable]
[DataDefinition]
public sealed partial class ChangelingRefreshEvent : EntityEventArgs
{
}
