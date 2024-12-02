using Content.Shared.Polymorph;

namespace Content.Server.Changeling;

[RegisterComponent]
public sealed partial class ForceTransformedComponent : Component
{
    [ViewVariables]
    public PolymorphHumanoidData? OriginalBody;

    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RevertAt;
}
