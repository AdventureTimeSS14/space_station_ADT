using Content.Server.ADT.Chemistry.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.ADT.Chemistry.Components
{
    /// <summary>
    /// A thing that shows reagents in it.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(ReagentAnalyzerSystem))]
    public sealed partial class ReagentAnalyzerComponent : Component
    {
        [DataField("clickSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
    }
}
