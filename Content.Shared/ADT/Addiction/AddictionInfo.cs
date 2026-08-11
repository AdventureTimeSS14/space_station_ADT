// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Serialization;

namespace Content.Shared.ADT.Addiction;

/// <summary>
/// Информация о зависимости для UI (анализатор здоровья): тип, стадия тяжести, неизлечимость.
/// </summary>
[Serializable, NetSerializable]
public struct AddictionInfo
{
    public AddictionKind Kind;
    public int Stage;
    public bool Permanent;

    public AddictionInfo(AddictionKind kind, int stage, bool permanent)
    {
        Kind = kind;
        Stage = stage;
        Permanent = permanent;
    }
}
