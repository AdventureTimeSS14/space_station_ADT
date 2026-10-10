//

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Heretic.Components.PathSpecific.Blade;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SacramentsOfPowerComponent : Component
{
    [DataField, AutoNetworkedField]
    public SacramentsState State = SacramentsState.Opening;

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> IgnoredEntities = new();

    [DataField]
    public TimeSpan StateUpdateAt;

    [DataField]
    public float DamageReturnRatio = 0.75f;

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/ADT/Heretic/ark_deathrattle.ogg");

    [DataField]
    public SoundSpecifier ActivationSound = new SoundPathSpecifier("/Audio/ADT/Heretic/piano_hit.ogg");

    [DataField]
    public TimeSpan ActivationTime = TimeSpan.FromSeconds(0.8);

    [DataField]
    public TimeSpan DeactivationTime = TimeSpan.FromSeconds(0.9);

    [DataField]
    public TimeSpan EffectTime = TimeSpan.FromSeconds(5);
}

public enum SacramentsState : byte
{
    Opening,
    Open,
    Closing,
}
