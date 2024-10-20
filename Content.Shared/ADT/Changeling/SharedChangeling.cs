using Content.Shared.Actions;
using Content.Shared.Changeling.Components;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
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

public sealed partial class OmniHealActionEvent : InstantActionEvent
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

    [Serializable]
    public struct HDATA
    {
        public NetEntity NetEntity;
        public string Name;
        public string Species;
        public HumanoidCharacterProfile Profile;
    }
    public RequestChangelingFormsMenuEvent(NetEntity target)
    {
        Target = target;
    }
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

    public SelectChangelingFormEvent(NetEntity target, NetEntity entitySelected)
    {
        Target = target;
        EntitySelected = entitySelected;
    }
}
