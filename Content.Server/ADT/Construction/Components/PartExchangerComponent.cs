// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Orion-Station-14 (PR #385, https://github.com/AtaraxiaSpaceFoundation/Orion-Station-14/pull/385)

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Construction.Components;

/// <summary>
/// Rapid Part Exchanger: lets the user swap machine parts in assembled machines (or finish a machine frame).
/// </summary>
[RegisterComponent]
public sealed partial class PartExchangerComponent : Component
{
    [DataField]
    public float ExchangeDuration = 3f;

    [DataField]
    public bool DoDistanceCheck = true;

    [DataField]
    public bool RequireOpenPanel = true;

    [DataField]
    public SoundSpecifier ExchangeSound = new SoundPathSpecifier("/Audio/Items/rped.ogg");

    [DataField]
    public EntProtoId? ExchangeBeamPrototype;
}
