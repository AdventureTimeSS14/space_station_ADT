using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Alert;
using Robust.Shared.Audio;

namespace Content.Shared.ADT.Phantom;

[ImplicitDataDefinitionForInheritors]
public partial interface IPhantomAbility
{
    public MindshieldAllowance MsAllowance { get; set; }

    public SoundSpecifier? Sound { get; set; }

    public enum MindshieldAllowance
    {
        Any,
        Malfunctioning,
        NoMindshield
    }
}

#region EntityTarget Actions
public sealed partial class MakeHolderActionEvent : EntityTargetActionEvent, IPhantomAbility
{
    [DataField]
    public IPhantomAbility.MindshieldAllowance MsAllowance { get; set; } = IPhantomAbility.MindshieldAllowance.Any;

    [DataField]
    public SoundSpecifier? Sound { get; set; }
}

public sealed partial class ParalysisActionEvent : EntityTargetActionEvent, IPhantomAbility
{
    [DataField]
    public IPhantomAbility.MindshieldAllowance MsAllowance { get; set; } = IPhantomAbility.MindshieldAllowance.NoMindshield;

    [DataField]
    public SoundSpecifier? Sound { get; set; } = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/blinding.ogg");

    [DataField]
    public float Duration = 10f;
}

public sealed partial class BreakdownActionEvent : EntityTargetActionEvent, IPhantomAbility
{
    [DataField]
    public IPhantomAbility.MindshieldAllowance MsAllowance { get; set; } = IPhantomAbility.MindshieldAllowance.Any;

    [DataField]
    public SoundSpecifier? Sound { get; set; }

    [DataField]
    public float StunDuration = 10f;

    [DataField]
    public float MalfDuration = 5f;
}

public sealed partial class RadioFakerActionEvent : EntityTargetActionEvent, IPhantomAbility
{
    [DataField]
    public IPhantomAbility.MindshieldAllowance MsAllowance { get; set; } = IPhantomAbility.MindshieldAllowance.Any;

    [DataField]
    public SoundSpecifier? Sound { get; set; }
}

public sealed partial class RepairActionEvent : EntityTargetActionEvent, IPhantomAbility
{
    [DataField]
    public IPhantomAbility.MindshieldAllowance MsAllowance { get; set; } = IPhantomAbility.MindshieldAllowance.Any;

    [DataField]
    public SoundSpecifier? Sound { get; set; }
}

public sealed partial class BloodBlindingActionEvent : EntityTargetActionEvent, IPhantomAbility
{
    [DataField]
    public IPhantomAbility.MindshieldAllowance MsAllowance { get; set; } = IPhantomAbility.MindshieldAllowance.Any;

    [DataField]
    public SoundSpecifier? Sound { get; set; } = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/blinding.ogg");
}
#endregion

#region Instant Actions
public sealed partial class MakeVesselActionEvent : InstantActionEvent, IPhantomAbility
{
    [DataField]
    public IPhantomAbility.MindshieldAllowance MsAllowance { get; set; } = IPhantomAbility.MindshieldAllowance.NoMindshield;

    [DataField]
    public SoundSpecifier? Sound { get; set; } = new SoundCollectionSpecifier("PhantomGhostKiss");
}


public sealed partial class HauntVesselActionEvent : InstantActionEvent, IPhantomAbility
{
    [DataField]
    public IPhantomAbility.MindshieldAllowance MsAllowance { get; set; } = IPhantomAbility.MindshieldAllowance.Any;

    [DataField]
    public SoundSpecifier? Sound { get; set; }
}

public sealed partial class OpenPhantomStylesMenuActionEvent : InstantActionEvent, IPhantomAbility
{
    [DataField]
    public IPhantomAbility.MindshieldAllowance MsAllowance { get; set; } = IPhantomAbility.MindshieldAllowance.Any;

    [DataField]
    public SoundSpecifier? Sound { get; set; }
}

public sealed partial class GhostClawsActionEvent : InstantActionEvent, IPhantomAbility
{
    [DataField]
    public IPhantomAbility.MindshieldAllowance MsAllowance { get; set; } = IPhantomAbility.MindshieldAllowance.NoMindshield;

    [DataField]
    public SoundSpecifier? Sound { get; set; }
}

public sealed partial class GhostInjuryActionEvent : InstantActionEvent, IPhantomAbility
{
    [DataField]
    public IPhantomAbility.MindshieldAllowance MsAllowance { get; set; } = IPhantomAbility.MindshieldAllowance.NoMindshield;

    [DataField]
    public SoundSpecifier? Sound { get; set; } = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/injury.ogg");
}

public sealed partial class GhostHealActionEvent : InstantActionEvent, IPhantomAbility
{
    [DataField]
    public IPhantomAbility.MindshieldAllowance MsAllowance { get; set; } = IPhantomAbility.MindshieldAllowance.Any;

    [DataField]
    public SoundSpecifier? Sound { get; set; } = new SoundPathSpecifier("/Audio/ADT/Phantom/Sounds/recovery.ogg");
}

public sealed partial class PhantomOathActionEvent : InstantActionEvent, IPhantomAbility
{
    [DataField]
    public IPhantomAbility.MindshieldAllowance MsAllowance { get; set; } = IPhantomAbility.MindshieldAllowance.NoMindshield;

    [DataField]
    public SoundSpecifier? Sound { get; set; }
}

public sealed partial class PhantomPortalActionEvent : InstantActionEvent, IPhantomAbility
{
    [DataField]
    public IPhantomAbility.MindshieldAllowance MsAllowance { get; set; } = IPhantomAbility.MindshieldAllowance.Any;

    [DataField]
    public SoundSpecifier? Sound { get; set; }
}

public sealed partial class PhantomHelpingHelpActionEvent : InstantActionEvent, IPhantomAbility
{
    [DataField]
    public IPhantomAbility.MindshieldAllowance MsAllowance { get; set; } = IPhantomAbility.MindshieldAllowance.NoMindshield;

    [DataField]
    public SoundSpecifier? Sound { get; set; }
}

public sealed partial class PhantomControlActionEvent : InstantActionEvent, IPhantomAbility
{
    [DataField]
    public IPhantomAbility.MindshieldAllowance MsAllowance { get; set; } = IPhantomAbility.MindshieldAllowance.NoMindshield;

    [DataField]
    public SoundSpecifier? Sound { get; set; }
}

public sealed partial class PsychoEpidemicActionEvent : InstantActionEvent
{
}
#endregion

#region Finale
public sealed partial class NightmareFinaleActionEvent : InstantActionEvent
{
}

public sealed partial class TyranyFinaleActionEvent : InstantActionEvent
{
}

public sealed partial class FreedomFinaleActionEvent : InstantActionEvent
{
}

public sealed partial class FreedomOblivionFinaleActionEvent : InstantActionEvent
{
}

public sealed partial class FreedomDeathmatchFinaleActionEvent : InstantActionEvent
{
}

public sealed partial class FreedomHelpFinaleActionEvent : InstantActionEvent
{
}

public enum PhantomFinaleType : byte
{
    Nightmare = 0,
    Tyrany = 1,
    Oblivion = 2,
    Deathmatch = 3,
    Help = 4
}
#endregion

#region DoAfter's
[Serializable, NetSerializable]
public sealed partial class MakeVesselDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class PuppeterDoAfterEvent : SimpleDoAfterEvent
{
}
#endregion

#region Events
[ByRefEvent]
public record struct RefreshPhantomLevelEvent();

[ByRefEvent]
public record struct PhantomReincarnatedEvent();

[ByRefEvent]
public record struct PhantomDiedEvent();

[ByRefEvent]
public record struct PhantomTyranyEvent();

[ByRefEvent]
public record struct PhantomNightmareEvent();

[ByRefEvent]
public record struct PhantomLevelReachedEvent(int Level);

[DataDefinition]
public sealed partial class EctoplasmHitscanHitEvent : EntityEventArgs
{
    [DataField(required: true)]
    public DamageSpecifier DamageToPhantom = new();

    [DataField(required: true)]
    public DamageSpecifier DamageToTarget = new();
}
#endregion

#region UI
/// <summary>
/// This event carries list of style prototypes and entity - the source of request. This class is a part of code which is responsible for using RadialUiController.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class RequestPhantomStyleMenuEvent : EntityEventArgs
{
    public readonly List<string> Prototypes = new();
    public NetEntity Target;

    public RequestPhantomStyleMenuEvent(NetEntity target)
    {
        Target = target;
    }
}

/// <summary>
/// This event carries prototype-id of style, which was selected. This class is a part of code which is responsible for using RadialUiController.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class SelectPhantomStyleEvent : EntityEventArgs
{
    public string PrototypeId;
    public NetEntity Target;
    public bool Handled = false;

    public SelectPhantomStyleEvent(NetEntity target, string prototypeId)
    {
        Target = target;
        PrototypeId = prototypeId;
    }
}

[Serializable, NetSerializable]
public sealed partial class RequestPhantomFreedomMenuEvent : EntityEventArgs
{
    public readonly List<EntProtoId> Prototypes = new();
    public NetEntity Target;

    public RequestPhantomFreedomMenuEvent(NetEntity target)
    {
        Target = target;
    }
}

[Serializable, NetSerializable]
public sealed partial class SelectPhantomFreedomEvent : EntityEventArgs
{
    public string PrototypeId;
    public NetEntity Target;
    public bool Handled = false;

    public SelectPhantomFreedomEvent(NetEntity target, string prototypeId)
    {
        Target = target;
        PrototypeId = prototypeId;
    }
}

[Serializable, NetSerializable]
public sealed partial class RequestPhantomVesselMenuEvent : EntityEventArgs
{
    public NetEntity Uid;
    public readonly List<(NetEntity, HumanoidCharacterProfile, string)> Vessels = new();

    public RequestPhantomVesselMenuEvent(NetEntity uid, List<(NetEntity, HumanoidCharacterProfile, string)> vessels)
    {
        Uid = uid;
        Vessels = vessels;
    }
}

[Serializable, NetSerializable]
public sealed partial class SelectPhantomVesselEvent : EntityEventArgs
{
    public NetEntity Uid;
    public NetEntity Vessel;

    public SelectPhantomVesselEvent(NetEntity uid, NetEntity vessel)
    {
        Uid = uid;
        Vessel = vessel;
    }
}

[Serializable, NetSerializable]
public sealed partial class PopulatePhantomVesselMenuEvent : EntityEventArgs
{
    public readonly List<(NetEntity, HumanoidCharacterProfile, string)> Vessels = new();
    public NetEntity Uid;

    public PopulatePhantomVesselMenuEvent(NetEntity uid, List<(NetEntity, HumanoidCharacterProfile, string)> vessels)
    {
        Uid = uid;
        Vessels = vessels;
    }
}

[Serializable, NetSerializable]
public sealed partial class OpenRadioFakerMenuEvent : EntityEventArgs
{
    public readonly List<string> Channels = new();
    public NetEntity User;
    public NetEntity Target;

    public OpenRadioFakerMenuEvent(NetEntity user, NetEntity target, List<string> channels)
    {
        User = user;
        Channels = channels;
        Target = target;
    }
}

[Serializable, NetSerializable]
public sealed partial class SendRadioFakerMessageEvent : EntityEventArgs
{
    public NetEntity User;
    public NetEntity Target;
    public string Message;
    public string Sender;
    public string Channel;

    public SendRadioFakerMessageEvent(NetEntity user, NetEntity target, string message, string sender, string channel)
    {
        User = user;
        Target = target;
        Message = message;
        Sender = sender;
        Channel = channel;
    }
}

#endregion

#region Visualizer
[NetSerializable, Serializable]
public enum PhantomVisuals : byte
{
    Corporeal,
    Stunned,
    Haunting,
}
#endregion

#region Puppets
public sealed partial class SelfGhostClawsActionEvent : InstantActionEvent
{
}

public sealed partial class SelfGhostHealActionEvent : InstantActionEvent
{
}
#endregion

public sealed partial class StopHauntAlertEvent : BaseAlertEvent;
