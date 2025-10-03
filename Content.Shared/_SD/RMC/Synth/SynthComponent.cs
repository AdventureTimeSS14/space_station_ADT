using Content.Shared.Body.Organ;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._SD.RMC.Synth;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSynthSystem))]
public sealed partial class SynthComponent : Component
{
    [DataField]
    public ComponentRegistry? AddComponents;

    [DataField]
    public ComponentRegistry? RemoveComponents;


    [DataField, AutoNetworkedField]
    public bool CanUseGuns = false;

    [DataField, AutoNetworkedField]
    public bool CanUseMeleeWeapons = true;

    /// <summary>
    /// The blood reagent to give the synth.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype> NewBloodReagent = "SD_SynthBlood";

    /// <summary>
    /// The damage modifier set to give the synth.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<DamageModifierSetPrototype> NewDamageModifier = "SD_Synth";

    [DataField, AutoNetworkedField]
    public LocId SpeciesName = "rmc-species-name-synth";

    /// <summary>
    /// I.E. 1st generation, 3rd generation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId Generation = "rmc-species-synth-generation-third";

    /// <summary>
    /// New brain organ to add when the synth is created.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId<OrganComponent> NewBrain = "SD_OrganSynthBrain";

    /// <summary>
    /// The time it takes to repair the synth.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RepairTime = TimeSpan.FromSeconds(0);

    /// <summary>
    /// The time it takes to repair the synth, if you are the synth.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan SelfRepairTime = TimeSpan.FromSeconds(30);


    [DataField, AutoNetworkedField]
    public string DamageVisualsColor = "#EEEEEE";
}
