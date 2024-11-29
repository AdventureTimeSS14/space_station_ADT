using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Neurotoxin;
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedNeurotoxinSystem))]
public sealed partial class NeurotoxinInjectorComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public float NeuroPerSecond;

    [DataField, AutoNetworkedField]
    public TimeSpan TimeBetweenGasInjects = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public bool AffectsDead;

    [DataField, AutoNetworkedField]
    public DamageSpecifier ToxinDamage = new();

    [DataField, AutoNetworkedField]
    public DamageSpecifier OxygenDamage = new();

    [DataField, AutoNetworkedField]
    public DamageSpecifier CoughDamage = new();

    [DataField, AutoNetworkedField]
    public bool InjectInContact = true;
}
