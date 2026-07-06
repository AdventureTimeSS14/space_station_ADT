using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.ADT.Shuttles.Components;

/// <summary>
/// A console that allows launching a drop pod at a chosen beacon on the station.
/// Must be placed on a grid that has <see cref="DropPodComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DropPodConsoleComponent : Component
{
    /// <summary>
    /// Beacon prototype IDs that cannot be targeted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> BeaconBlacklist = new()
    {
        "DefaultStationBeaconBridge",
        "DefaultStationBeaconVault",
        "DefaultStationBeaconCaptainsQuarters",
        "DefaultStationBeaconHOPOffice",
        "DefaultStationBeaconHOSRoom",
        "DefaultStationBeaconCMORoom",
        "DefaultStationBeaconRDRoom",
        "DefaultStationBeaconQMRoom",
        "DefaultStationBeaconCERoom",
        "DefaultStationBeaconSecurity",
        "DefaultStationBeaconBrig",
        "DefaultStationBeaconBrigMed",
        "DefaultStationBeaconWardensOffice",
        "DefaultStationBeaconArmory",
        "DefaultStationBeaconPermaBrig",
        "DefaultStationBeaconDetectiveRoom",
        "DefaultStationBeaconCourtroom",
        "DefaultStationBeaconLawOffice",
        "DefaultStationBeaconSecurityCheckpoint",
        "ADTDefaultStationBeaconDockingAreaSec",
        "ADTDefaultStationBeaconSuitsSec",
        "DefaultStationBeaconAI",
        "DefaultStationBeaconAISatellite",
        "DefaultStationBeaconAICore",
        "DefaultStationBeaconAIUpload",
        "DefaultStationBeaconAIPower",
        "ADTDefaultStationBeaconSuperMatter",
        "DefaultStationBeaconGravGen",
        "DefaultStationBeaconAnchor",
        "DefaultStationBeaconSingularity",
        "DefaultStationBeaconTelecoms",
        "DefaultStationBeaconTEG",
        "DefaultStationBeaconNuclear",
        "DefaultStationBeaconNukeOps",
        "NuclearBomb",
        "ADTBluespaceHarvesterRift",
        "DefaultStationBeaconCentComm",
    };

    /// <summary>
    /// Total flight time in seconds from launch to impact. Also used as the announcement lead time.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FlightTime = 90f;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(120);

    public TimeSpan LastLaunchTime = TimeSpan.Zero;

    /// <summary>
    /// Prototype spawned at the landing site before the pod arrives.
    /// Null disables the effect entirely.
    /// </summary>
    [DataField]
    public string? PreLandingSpawnPrototype = "ADTDroppodTarget";

    /// <summary>
    /// How many seconds before landing to spawn <see cref="PreLandingSpawnPrototype"/>.
    /// </summary>
    [DataField]
    public float PreLandingSpawnLeadTime = 15f;

    /// <summary>
    /// Cost of launching the drop pod in peacetime.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int PeaceCost = 60;

    /// <summary>
    /// Cost of launching the drop pod during war (discounted).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int WarCost = 30;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? WarDeclaredTime;
}

[Serializable, NetSerializable]
public enum DropPodConsoleUiKey : byte { Key }
