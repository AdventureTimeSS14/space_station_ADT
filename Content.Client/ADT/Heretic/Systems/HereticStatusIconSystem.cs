using Content.Shared.Heretic.Components;
using Content.Shared.StatusIcon;
using Robust.Shared.Utility;

namespace Content.Client.ADT.Heretic.Systems;

public sealed class HereticStatusIconSystem : EntitySystem
{
    public static readonly StatusIconData HereticIcon = new()
    {
        Icon = new SpriteSpecifier.Rsi(new("ADT/Heretic/Interface/Icons/heretic.rsi"), "heretic"),
        Priority = 5,
    };

    public static readonly StatusIconData GhoulIcon = new()
    {
        Icon = new SpriteSpecifier.Rsi(new("ADT/Heretic/Interface/Icons/heretic.rsi"), "ghoul_voiceless"),
        Priority = 6,
    };

    public static readonly StatusIconData ShatteredIcon = new()
    {
        Icon = new SpriteSpecifier.Rsi(new("ADT/Heretic/Interface/Icons/heretic.rsi"), "ghoul_shattered"),
        Priority = 6,
    };
}