using Content.Shared.DoAfter;
using Content.Shared.Explosion;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;
using Content.Shared.Standing;
using Robust.Shared.Serialization;
using Content.Shared.Stunnable;
using Robust.Shared.Player;
using Content.Shared.Movement.Systems;
using Content.Shared.Alert;
using Content.Shared.Climbing.Components;
using Content.Shared.Popups;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Map.Components;
using Content.Shared.Climbing.Systems;
using Content.Shared.Climbing.Events;

namespace Content.Shared.ADT.Crawling;

[ByRefEvent]
public record struct ExplosionDownAttemptEvent(string Explosion, bool Cancelled = false);
