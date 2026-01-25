using Content.Shared.ADT.Language;
using Content.Shared.Roles;
using JetBrains.Annotations;

namespace Content.Server.ADT.Language;

/// <summary>
///     Добавляет язык(и) персонажу при получении работы.
///     Не заменяет существующие языки, а добавляет к ним.
/// </summary>
[UsedImplicitly]
public sealed partial class AddLanguageSpecial : JobSpecial
{
    [DataField]
    public Dictionary<string, LanguageKnowledge> Languages { get; private set; } = new();

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var langSystem = entMan.System<LanguageSystem>();

        foreach (var (lang, knowledge) in Languages)
        {
            langSystem.AddSpokenLanguage(mob, lang, knowledge);
        }
    }
}
