// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).

using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Shaders;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class NaturalNightVisionComponent : Component
{
    [DataField("brightnessGain"), AutoNetworkedField]
    public float BrightnessGain = 3.0f;

    [DataField("contrast"), AutoNetworkedField]
    public float Contrast = 1.3f;

    [DataField("tintColor"), AutoNetworkedField]
    public Color TintColor = Color.FromHex("#1c89f2");

    [DataField("visionRadius"), AutoNetworkedField]
    public float VisionRadius = 6.0f;

    [DataField("visible"), AutoNetworkedField]
    public bool Visible = false;

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    public EntProtoId ActionProto = "ActionToggleNaturalNightVision";
}

public sealed partial class ToggleNaturalNightVisionEvent : InstantActionEvent
{
}
