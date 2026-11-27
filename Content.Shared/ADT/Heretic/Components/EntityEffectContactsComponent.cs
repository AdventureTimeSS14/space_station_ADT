//

using Content.Shared.EntityConditions;
using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;

namespace Content.Shared.Heretic.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class EntityEffectContactsComponent : Component
{
    [DataField(required: true)]
    public string Id = string.Empty;

    [DataField(required: true)]
    public EntityEffect[] Effects = [];

    [DataField]
    public EntityCondition[]? Conditions;
}

[NetworkedComponent, RegisterComponent]
public sealed partial class EntityEffectContactsAffectedComponent : Component
{
    [DataField]
    public Dictionary<string, EntityUid> Contacts = new();
}
