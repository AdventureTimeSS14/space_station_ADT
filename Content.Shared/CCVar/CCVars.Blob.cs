using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    // ADT-Tweak-Start: ADT Blob
    /// <summary>
    ///     Whether blob tiles are allowed to spread onto space tiles.
    /// </summary>
    public static readonly CVarDef<bool> BlobCanGrowInSpace =
        CVarDef.Create("blob.grow_space", true, CVar.SERVER);
    // ADT-Tweak-End
}
