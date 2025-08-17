using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Ghost
{

    [Serializable, NetSerializable]
    public sealed class GhostInfoUpdateState : BoundUserInterfaceState
    {
        public string? StationName;
        public string? StationAlertLevel;
        public Color StationAlertColor;
        /// <summary>
        /// Инициализирует новое состояние интерфейса обновления информации для призраков с заданным именем станции, уровнем тревоги и цветом тревоги.
        /// </summary>
        /// <param name="stationName">Имя станции. Может быть <c>null</c>, если имя не задано.</param>
        /// <param name="stationAlertLevel">Текстовое обозначение уровня тревоги станции. Может быть <c>null</c>, если уровень не указан.</param>
        /// <param name="stationAlertColor">Цвет, соответствующий уровню тревоги.</param>
        public GhostInfoUpdateState(string? stationName, string? stationAlertLevel, Color stationAlertColor)
        {
            StationName = stationName;
            StationAlertLevel = stationAlertLevel;
            StationAlertColor = stationAlertColor;
        }
    }
}
