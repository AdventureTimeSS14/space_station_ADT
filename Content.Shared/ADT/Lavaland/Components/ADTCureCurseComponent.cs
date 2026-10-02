using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Lavaland.Components;

[RegisterComponent]
public sealed partial class ADTCureCurseComponent : Component
{
    [DataField]
    public bool Active;

    [DataField]
    public LocId ActivateSpeech = "adt-cure-curse-activate-speech";
}

[Serializable, NetSerializable]
public enum ADTCureCurseVisuals : byte
{
    Active,
}
