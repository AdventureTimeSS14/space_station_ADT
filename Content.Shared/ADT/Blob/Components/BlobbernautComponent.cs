// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.ADT.Blob;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.ADT.Blob.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(SharedBlobbernautSystem))]
public sealed partial class BlobbernautComponent : Component
{
    [DataField("color"), AutoNetworkedField]
    [Access(Other = AccessPermissions.ReadWrite)]
    public Color Color = Color.White;

    [ViewVariables(VVAccess.ReadWrite), DataField("damageFrequency")]
    public float DamageFrequency = 5;

    [ViewVariables(VVAccess.ReadOnly)]
    public float NextDamage = 0;

    [ViewVariables(VVAccess.ReadOnly), DataField("damage")]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>
        {
            { "Piercing", 25 },
        }
    };

    [ViewVariables(VVAccess.ReadOnly)]
    [Access(Other = AccessPermissions.ReadWrite)]
    public EntityUid? Factory = default!;
}
