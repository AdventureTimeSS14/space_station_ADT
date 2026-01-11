using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.BarbellBench;

/// <summary>
/// Позволяет предмету при использовании в руке запускать эмоут "поднимает штангу" и тратить выносливость.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BarbellLiftComponent : Component
{
    /// <summary>
    /// Урон по выносливости, наносимый пользователю за один подъём.
    /// </summary>
    [DataField("staminaCost"), AutoNetworkedField]
    public float StaminaCost = 15f;

    /// <summary>
    /// Ключ локализации текста эмоуты (текст уже содержит имя через параметр {$name}).
    /// </summary>
    [DataField("emoteLoc"), AutoNetworkedField]
    public string EmoteLoc = "adt-barbell-lift-emote";

    /// <summary>
    /// Ключ локализации текста эмоуты для самого игрока.
    /// </summary>
    [DataField("emoteLocSelf"), AutoNetworkedField]
    public string EmoteLocSelf = "adt-barbell-lift-emote-self";

    /// <summary>
    /// Optional RSI path override for barbell bench overlay visuals when this barbell is attached.
    /// If null, the bench will use its own overlay sprite RSI.
    /// </summary>
    [DataField("benchOverlayRsi"), AutoNetworkedField]
    public ResPath? BenchOverlayRsi;

    /// <summary>
    /// Base/idle overlay state (shown when attached to bench).
    /// </summary>
    [DataField("benchOverlayBaseState"), AutoNetworkedField]
    public string BenchOverlayBaseState = "barbell-overlay-up";

    /// <summary>
    /// Flick state used during a rep animation.
    /// </summary>
    [DataField("benchOverlayRepFlickState"), AutoNetworkedField]
    public string BenchOverlayRepFlickState = "barbell-up";
}


