using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Climbing.Events;
using Robust.Shared.Network;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Content.Shared.Tools.Components;
using Content.Shared.StepTrigger.Systems;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.StepTrigger.Components;
using Content.Shared.Implants;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Content.Shared.Weapons.Melee;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Implants;

#region Actions
public sealed partial class ToggleMantisDaggersEvent : InstantActionEvent
{
}

public sealed partial class ToggleSundownerShieldsEvent : InstantActionEvent
{
}

public sealed partial class ToggleMusclesEvent : InstantActionEvent
{
}

public sealed partial class ToggleCompStealthEvent : InstantActionEvent
{
}
#endregion

#region Visualizer
[NetSerializable, Serializable]
public enum MantisDaggersVisuals : byte
{
    Inactive,
    Active,
}

[NetSerializable, Serializable]
public enum MistralFistsVisuals : byte
{
    Inactive,
    Active,
}

[NetSerializable, Serializable]
public enum SundownerShieldsVisuals : byte
{
    Open,
    Closed,
}
#endregion
