using Content.Shared.Supermatter;
using Robust.Shared.Configuration;
using Content.Shared.Supermatter.Components;
using Content.Shared.Maps;
using Content.Shared.Roles;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Physics.Components;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
            /// <summary>
            ///     Toggles cascade
            /// </summary>
            public static readonly CVarDef<bool> SupermatterDoCascadeDelam =
             CVarDef.Create("supermatter.do_cascade", true, CVar.SERVER);

            /// <summary>
            ///     Directly multiplies the amount of rads put out by the supermatter. Be VERY conservative with this.
            /// </summary>
            public static readonly CVarDef<float> SupermatterRadsModifier =
             CVarDef.Create("supermatter.rads_modifier", 1f, CVar.SERVER);
}
