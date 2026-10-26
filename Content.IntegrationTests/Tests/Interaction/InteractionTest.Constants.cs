using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Interaction;

// This partial class contains various constant prototype IDs common to interaction tests.
// Should make it easier to mass-change hard coded strings if prototypes get renamed.
public abstract partial class InteractionTest
{
    // Tiles
    protected const string Floor = "FloorSteel";
    protected const string FloorItem = "FloorTileItemSteel";
    protected const string Plating = "Plating";
    protected const string PlatingRCD = "PlatingRCD";
    protected const string Lattice = "Lattice";
    protected const string PlatingBrass = "PlatingBrass";

    // Structures
    protected const string Airlock = "Airlock";

    // Tools/steps
    protected const string Wrench = "Wrench";
    protected const string Screw = "Screwdriver";
    protected const string Weld = "WelderExperimental";
    protected const string Pry = "Crowbar";
    protected const string Cut = "Wirecutter";

    // Materials/stacks
    protected const string Steel = "Steel";
    protected const string Glass = "Glass";
    protected const string RGlass = "ReinforcedGlass";
    protected const string Plastic = "Plastic";
    protected const string Cable = "Cable";
    protected const string Rod = "MetalRod";

    // Parts
    // ADT-Tweak start: machine parts
    protected const string Servo1 = "MicroServoStockPart";
    protected const string Servo2 = "NanoServoStockPart";
    protected const string Servo3 = "PicoServoStockPart";
    protected const string Servo4 = "FemtoServoStockPart";
    protected const string MatterBin1 = "MatterBinStockPart";
    protected const string MatterBin2 = "AdvancedMatterBinStockPart";
    protected const string MatterBin3 = "SuperMatterBinStockPart";
    protected const string MatterBin4 = "BluespaceMatterBinStockPart";
    protected const string Capacitor1 = "CapacitorStockPart";
    protected const string Capacitor2 = "AdvancedCapacitorStockPart";
    protected const string Capacitor3 = "SuperCapacitorStockPart";
    protected const string Capacitor4 = "BluespaceCapacitorStockPart";
    protected const string MicroLaser1 = "MicroLaserStockPart";
    protected const string MicroLaser2 = "HighPowerMicroLaserStockPart";
    protected const string MicroLaser3 = "UltraHighPowerMicroLaserStockPart";
    protected const string MicroLaser4 = "QuadUltraMicroLaserStockPart";
    protected const string ScanningModule1 = "ScanningModuleStockPart";
    protected const string ScanningModule2 = "AdvancedScanningModuleStockPart";
    protected const string ScanningModule3 = "PhasicScanningModuleStockPart";
    protected const string ScanningModule4 = "TriphasicScanningModuleStockPart";
    protected const string RapidPartExchanger = "RapidPartExchanger";
    protected const string BluespaceRapidPartExchanger = "BluespaceRapidPartExchanger";
    // ADT-Tweak end: machine parts
    protected const string Battery1 = "PowerCellSmall";
    protected const string Battery4 = "PowerCellHyper";

    // Inflatables & Needle used to pop them
    protected static readonly EntProtoId InflatableWall = "InflatableWall";
    protected static readonly EntProtoId Needle = "WeaponMeleeNeedle";
    protected static readonly ProtoId<StackPrototype> InflatableWallStack = "InflatableWall";
}
