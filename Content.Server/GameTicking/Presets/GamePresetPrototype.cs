using Content.Server.Maps;
using Robust.Shared.Prototypes;

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

        [DataField]
        public string[] Alias = Array.Empty<string>();

        [DataField("name")]
        public string ModeTitle = "????";

        [DataField]
        public string Description = string.Empty;

        [DataField]
        public bool ShowInVote;

        [DataField]
        public int? MinPlayers;

        [DataField]
        public int? MaxPlayers;

        [DataField]
        public IReadOnlyList<EntProtoId> Rules { get; private set; } = Array.Empty<EntProtoId>();

        /// <summary>
        /// If specified, the gamemode will only be run with these maps.
        /// If none are elligible, the global fallback will be used.
        /// </summary>
<<<<<<< HEAD
        [DataField("supportedMaps", customTypeSerializer: typeof(PrototypeIdSerializer<GameMapPoolPrototype>))]
        public string? MapPool;

        //ADT-Tweak-Start
        /// <summary>
        /// Количество раундов, на которое этот игровой режим будет заблокирован после его использования.
        /// </summary>
        /// <remarks>
        /// Если значение не задано (<c>null</c>) или равно 0, режим не блокируется и может появляться в голосовании каждую игру.
        /// Если значение больше 0, то после запуска этого режима он будет недоступен указанное количество следующих раундов.
        /// </remarks>
        /// <example>
        /// Например, при значении <c>2</c> режим будет отсутствовать в голосовании в течение двух следующих раундов,
        /// а затем снова появится.
        /// </example>
        [DataField]
        public int? BannedRound = 0;
        //ADT-Tweak-End
=======
        [DataField("supportedMaps")]
        public ProtoId<GameMapPoolPrototype>? MapPool;
>>>>>>> wizards-filtered
    }
}
