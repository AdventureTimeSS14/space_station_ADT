//

using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Heretic.Components.PathSpecific.Blade;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChampionHookComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Weapon;

    [DataField, AutoNetworkedField]
    public EntityUid? HookedMob;

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(2);

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/ADT/Heretic/parry.ogg");
}
