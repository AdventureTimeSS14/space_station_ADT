using Robust.Shared.Prototypes;

namespace Content.Shared.ADT.Administration
{
    /// <summary>
    ///     Describes information for a single admin test arena.
    /// </summary>
    [Prototype("adminArenaVerb")]
    public sealed partial class AdminArenaVerbPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        /// <summary>
        ///     Name of this admin arena displayed to players.
        /// </summary>
        [DataField("name")]
        public string Name { get; private set; } = string.Empty;

        [ViewVariables(VVAccess.ReadOnly)]
        public string LocalizedName => Loc.GetString(Name);

        /// <summary>
        ///     Optional description for the admin arena.
        /// </summary>
        [DataField("description")]
        public string? Description { get; private set; }

        /// <summary>
        ///     The path to the map file for the admin arena.
        /// </summary>
        [DataField("pathMap", required: true)]
        public string PathMap { get; private set; } = string.Empty;

        /// <summary>
        ///     The icon sprite path for this arena.
        /// </summary>
        [DataField("iconAltVerb", required: true)]
        public string IconAltVerb { get; private set; } = string.Empty;
    }
}

/*
    ADT Content by 🐾 Schrödinger's Code 🐾
    ╔════════════════════════════════════╗
    ║   Schrödinger's Cat Code   🐾      ║
    ║   /\_/\\                           ║
    ║  ( o.o )  Meow!                    ║
    ║   > ^ <                            ║
    ╚════════════════════════════════════╝
*/
