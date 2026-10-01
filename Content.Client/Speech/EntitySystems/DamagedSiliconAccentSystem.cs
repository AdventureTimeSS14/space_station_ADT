using Content.Shared.Speech.Components;
using Content.Shared.Speech.EntitySystems;

namespace Content.Client.Speech.EntitySystems;

<<<<<<< HEAD:Content.Client/Speech/EntitySystems/SlurredSystem.cs
public sealed class SlurredSystem : SharedSlurredSystem
{
    protected override string AccentuateInternal(EntityUid uid, SlurredAccentComponent comp, string message)
    {
        return message;
    }
}
=======
public sealed partial class DamagedSiliconAccentSystem : SharedDamagedSiliconAccentSystem;
>>>>>>> wizards-filtered:Content.Client/Speech/EntitySystems/DamagedSiliconAccentSystem.cs
