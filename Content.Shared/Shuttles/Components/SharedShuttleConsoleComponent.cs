using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Shuttles.Components
{
    /// <summary>
    /// Interact with to start piloting a shuttle.
    /// </summary>
    [NetworkedComponent]
    public abstract partial class SharedShuttleConsoleComponent : Component
    {
        public static string DiskSlotName = "disk_slot";

        // ADT-TWEAK-START
        [DataField("isHandheldConsole")]
        public bool IsHandheldConsole = false;
        // ADT-TWEAK-END

    }

    [Serializable, NetSerializable]
    public enum ShuttleConsoleUiKey : byte
    {
        Key,
    }
}
