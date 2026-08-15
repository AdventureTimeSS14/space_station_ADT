using Robust.Shared.GameStates;

namespace Content.Shared.ADT.DrunkDrift;

/// <summary>Маркер пьяного: клиент шатает движение.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ADTDrunkDriftComponent : Component
{
    /// <summary>Эффекты активны: остаток опьянения выше порога размытия.</summary>
    [DataField, AutoNetworkedField]
    public bool VisualsActive;

    /// <summary>Порог остатка опьянения для эффектов, как в DrunkOverlay.</summary>
    [DataField]
    public TimeSpan VisualThreshold = TimeSpan.FromSeconds(50);

    /// <summary>Амплитуда плавного покачивания (радианы).</summary>
    [DataField, AutoNetworkedField]
    public float SwayAmplitude = 0.12f;

    /// <summary>Шанс рывка на каждом интервале (0..1).</summary>
    [DataField, AutoNetworkedField]
    public float LurchChance = 0.35f;

    /// <summary>Сила рывка (радианы).</summary>
    [DataField, AutoNetworkedField]
    public float LurchAngle = 0.25f;

    /// <summary>Интервал рывков (сек).</summary>
    [DataField, AutoNetworkedField]
    public float LurchInterval = 3f;
}
