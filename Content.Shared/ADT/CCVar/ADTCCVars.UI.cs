using Robust.Shared.Configuration;

namespace Content.Shared.ADT.CCVar;

public sealed partial class ADTCCVars
{
    public static readonly CVarDef<int> ChatSeparatedMinWidth =
        CVarDef.Create("ui.seperated_chat_min_width", 300, CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<string> RangedSight =
        CVarDef.Create("ui.ranged_sight", "GunSight", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> RangedSightScale =
        CVarDef.Create("ui.ranged_sight_scale", 60, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> RangedSightOffset =
        CVarDef.Create("ui.ranged_sight_offset", 50, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> SightMainColor =
        CVarDef.Create("ui.sight_main_color", Color.White.WithAlpha(0.3f).ToHex(), CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> SightSecondColor =
        CVarDef.Create("ui.sight_second_color", Color.Black.WithAlpha(0.5f).ToHex(), CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> MeleeSight =
        CVarDef.Create("ui.melee_sight", "MeleeSight", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> WieldBeforeRack =
        CVarDef.Create("ui.wield_before_rack", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RangedSightRotation =
        CVarDef.Create("ui.ranged_sight_rotation", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> HeldItemShowMode =
        CVarDef.Create("ui.held_item_show_mode", (int) Content.Shared.ADT.CCVar.HeldItemShowMode.Always, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> SightShowBoltIndicator =
        CVarDef.Create("ui.sight_show_bolt_indicator", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> SightShowHeatIndicator =
        CVarDef.Create("ui.sight_show_heat_indicator", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> SightShowJamIndicator =
        CVarDef.Create("ui.sight_show_jam_indicator", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> MeleeSightRotation =
        CVarDef.Create("ui.melee_sight_rotation", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}

public enum HeldItemShowMode
{
    Always = 0,
    CombatModeOnly = 1,
    OutsideCombatModeOnly = 2,
    Never = 3,
}
