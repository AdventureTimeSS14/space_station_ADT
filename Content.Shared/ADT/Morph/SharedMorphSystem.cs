using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Inventory.Events;
using Content.Shared.Rounding;
using Content.Shared.Toggleable;
using Robust.Shared.Timing;
using Content.Shared.ADT.Eye.Blinding;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Power.Generator;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Content.Shared.Chemistry.Components;
using Content.Shared.Devour;
using Content.Shared.Devour.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Content.Shared.Weapons.Marker;
using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Robust.Shared.Physics.Events;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Morph;

public abstract class SharedMorphSystem : EntitySystem
{
    public override void Initialize()
    {
    }
}
[Serializable, NetSerializable]
public sealed partial class MorphDevourDoAfterEvent : SimpleDoAfterEvent { }
