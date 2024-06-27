using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using Content.Shared.Alert;

namespace Content.Shared.Changeling.Components;

[RegisterComponent]
[AutoGenerateComponentState(true)]
public sealed partial class LingEggsHolderComponent : Component
{
    [DataField]
    public EntProtoId ChangelingHatchAction = "ActionLingHatch";

    [DataField, AutoNetworkedField]
    public EntityUid? ChangelingHatchActionEntity;

    [DataField]
    public float DamageAmount = 500f;    /// Damage gain to die

    public Container Stomach = default!;
    [DataField]
    public ProtoId<AlertPrototype> Alert = "ADTAlertApathy";
}
