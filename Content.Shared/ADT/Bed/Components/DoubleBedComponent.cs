using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared.ADT.Bed.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DoubleBedComponent : Component
{
    [DataField, AutoNetworkedField]
    public Vector2 LeftOffset = new Vector2(0f, -0.25f);

    [DataField, AutoNetworkedField]
    public Vector2 RightOffset = new Vector2(0f, 0.25f);

    [DataField, AutoNetworkedField]
    public Vector2 LeftBedsheetOffset = new Vector2(0f, 0.5f);

    [DataField, AutoNetworkedField]
    public Vector2 RightBedsheetOffset = new Vector2(0f, 0f);
}
