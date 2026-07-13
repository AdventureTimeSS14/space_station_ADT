using Content.Server.Heretic.EntitySystems;
using Content.Shared.Eye;

namespace Content.Server.Heretic.Components;

[RegisterComponent, Access(typeof(EldritchInfluenceSystem))]
public sealed partial class EldritchInfluenceComponent : Component
{
    [DataField] public bool Spent = false;

    [NonSerialized] public static int LayerMask = (int)VisibilityFlags.Eldritch;
}
