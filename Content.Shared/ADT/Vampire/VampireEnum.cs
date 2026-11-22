// SPDX-FileCopyrightText: 2021-2026 Corvax
// SPDX-FileCopyrightText: 2024-2026 Wega Team
// SPDX-FileCopyrightText: 2026 Adventure Time SS14 contributors
//
// SPDX-License-Identifier: GPL-3.0-only
//
// Ported from Wega (https://github.com/wega-team/ss14-wega).
// Modified by Adventure Time SS14 in 2026

using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Vampire;

[Serializable, NetSerializable]
public enum VampireClassEnum : byte
{
    NonSelected = 0,
    Hemomancer,
    Umbrae,
    Gargantua,
    Dantalion,
    Bestia, // Here! Don't bite me!
    Ancient
}

[Serializable, NetSerializable]
public enum VampireVariant : byte
{
    Classic = 0,
    Goon,
    Devil
}

[Serializable, NetSerializable]
public enum VampireVisualLayers : byte
{
    Digit1,
    Digit2,
    Digit3,
    Digit4
}

// Bestia enum
[Serializable, NetSerializable]
public enum BestiaOrganType : byte
{
    Unknown = 0,
    Heart,
    Lungs,
    Liver,
    Kidneys,
    Eyes,
    Stomach,
    Ears
}
