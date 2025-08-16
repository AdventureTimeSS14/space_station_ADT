
using Content.Server.Maps;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.GameTicking.Presets
{
    /// <summary>
    ///     A round-start setup preset, such as which antagonists to spawn.
    /// </summary>
    [Prototype]
    public sealed partial class GamePresetPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField("alias")]
        public string[] Alias = Array.Empty<string>();

        [DataField("name")]
        public string ModeTitle = "????";

        [DataField("description")]
        public string Description = string.Empty;

        [DataField("showInVote")]
        public bool ShowInVote;

        [DataField("minPlayers")]
        public int? MinPlayers;

        [DataField("maxPlayers")]
        public int? MaxPlayers;

        [DataField("rules", customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
        public IReadOnlyList<string> Rules { get; private set; } = Array.Empty<string>();

        /// <summary>
        /// If specified, the gamemode will only be run with these maps.
        /// If none are elligible, the global fallback will be used.
        /// </summary>
        [DataField("supportedMaps", customTypeSerializer: typeof(PrototypeIdSerializer<GameMapPoolPrototype>))]
        public string? MapPool;

        /// <summary>
        /// ADT. Количество раундов, на которое этот игровой режим будет заблокирован после его использования.
        /// </summary>
        /// <remarks>
        /// Если значение не задано (<c>null</c>) или равно 0, режим не блокируется и может появляться в голосовании каждую игру.
        /// Если значение больше 0, то после запуска этого режима он будет недоступен указанное количество следующих раундов.
        /// </remarks>
        /// <example>
        /// Например, при значении <c>2</c> режим будет отсутствовать в голосовании в течение двух следующих раундов,
        /// а затем снова появится.
        /// </example>
        [DataField("bannedRound")]
        public int? BannedRound = 0;
    }
}
