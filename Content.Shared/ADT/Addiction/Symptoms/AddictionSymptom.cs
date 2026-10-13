// SPDX-FileCopyrightText: 2026 ultradyper <ultradyper@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using JetBrains.Annotations;

namespace Content.Shared.ADT.Addiction.Symptoms;

/// <summary>
/// Симптом ломки: что держится на мобе, пока идёт ломка нужной стадии.
/// Накладывается и продлевается, пока стадия его требует, и снимается, когда перестаёт.
/// </summary>
[ImplicitDataDefinitionForInheritors, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class AddictionSymptom
{
    /// <summary>
    /// Чем симптом владеет на мобе. Симптомы с одинаковым ключом не складываются:
    /// из всех каналов в ломке побеждает тот, что пришёл с более тяжёлой стадии.
    /// Ключ должен быть стабилен: по нему симптом потом снимают.
    /// </summary>
    public abstract string Key { get; }

    /// <summary>
    /// Наложить или продлить симптом.
    /// Возвращает false, если симптом решил не трогать моба (например, компонент уже стоит
    /// не от ломки) - тогда ломка его не присваивает и при окончании не снимет.
    /// </summary>
    public abstract bool Apply(AddictionSymptomContext ctx);

    /// <summary>
    /// Снять симптом. Вызывается только для того, что ломка реально наложила сама.
    /// </summary>
    public abstract void Remove(AddictionSymptomContext ctx);
}

/// <summary>
/// Контекст применения симптома. Owned говорит, держит ли ломка этот симптом уже сейчас
/// (повторный вызов на продление), чтобы симптом отличал продление от первого наложения.
/// </summary>
public readonly record struct AddictionSymptomContext(
    EntityUid Target,
    IEntityManager EntMan,
    bool Owned);
