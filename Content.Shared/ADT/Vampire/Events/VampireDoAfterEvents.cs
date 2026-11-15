// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
//
// SPDX-License-Identifier: GPL-3.0-only

using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Vampire;

[Serializable, NetSerializable]
public sealed partial class VampireDrinkingBloodDoAfterEvent : SimpleDoAfterEvent
{
    public float Volume = 5;
    public float AbsorptionRatio = 0.33f;
}

[Serializable, NetSerializable]
public sealed partial class SoulAnchorDoAfterEvent : SimpleDoAfterEvent
{
    public EntProtoId EntityId = "BeaconSoul";
    public FixedPoint2 BloodCost { get; }

    public SoulAnchorDoAfterEvent(FixedPoint2 cost)
    {
        BloodCost = cost;
    }
}

[Serializable, NetSerializable]
public sealed partial class EnthrallDoAfterEvent : SimpleDoAfterEvent
{
    public FixedPoint2 BloodCost { get; }

    public EnthrallDoAfterEvent(FixedPoint2 cost)
    {
        BloodCost = cost;
    }
}

[Serializable, NetSerializable]
public sealed partial class VampireDissectDoAfterEvent : SimpleDoAfterEvent
{
    public FixedPoint2 BloodCost;
    public SoundSpecifier DissectSound = new SoundCollectionSpecifier("Organ");

    public VampireDissectDoAfterEvent(FixedPoint2 cost)
    {
        BloodCost = cost;
    }
}
