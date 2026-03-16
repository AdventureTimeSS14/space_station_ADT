using System.Linq;
using Content.Server.Administration;
using Content.Server.ADT.Language;
using Content.Shared.ADT.Language;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class AddLanguageCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntitySystemManager _systemManager = default!;

    public override string Command => "languageadd";

    public override string Description => Loc.GetString("cmd-languageadd-desc");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!int.TryParse(args[0], out var entInt))
        {
            shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        var languageId = args[1];

        var knowledge = LanguageKnowledge.Speak;
        if (args.Length >= 3)
        {
            if (!Enum.TryParse(args[2], true, out LanguageKnowledge parsedKnowledge)
                || !Enum.IsDefined(parsedKnowledge))
            {
                shell.WriteLine(Loc.GetString("cmd-languageadd-invalid-knowledge"));
                return;
            }
            knowledge = parsedKnowledge;
        }

        var nent = new NetEntity(entInt);

        if (!EntityManager.TryGetEntity(nent, out var uid))
        {
            shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
            return;
        }

        var uidVal = uid.Value;

        if (!EntityManager.HasComponent<LanguageSpeakerComponent>(uidVal))
        {
            EntityManager.AddComponent<LanguageSpeakerComponent>(uidVal);
        }

        var languageComponent = EntityManager.GetComponent<LanguageSpeakerComponent>(uidVal);

        if (languageId.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var allLanguages = _prototypeManager.EnumeratePrototypes<LanguagePrototype>().ToList();
            var addedCount = 0;

            foreach (var langProto in allLanguages)
            {
                if (languageComponent.Languages.ContainsKey(langProto.ID))
                {
                    languageComponent.Languages[langProto.ID] = knowledge;
                }
                else
                {
                    languageComponent.Languages.Add(langProto.ID, knowledge);
                }
                addedCount++;
            }

            EntityManager.Dirty(uidVal, languageComponent);

            var languageSystem = _systemManager.GetEntitySystem<LanguageSystem>();
            languageSystem.UpdateUi(uidVal, languageComponent);

            shell.WriteLine(Loc.GetString("cmd-languageadd-all-success",
                ("entity", uidVal.ToString()),
                ("count", addedCount),
                ("knowledge", knowledge)));
            return;
        }

        if (!_prototypeManager.TryIndex<LanguagePrototype>(languageId, out var languageProto))
        {
            shell.WriteLine(Loc.GetString("cmd-languageadd-invalid-language", ("language", languageId)));
            return;
        }

        if (languageComponent.Languages.ContainsKey(languageId))
        {
            languageComponent.Languages[languageId] = knowledge;
        }
        else
        {
            languageComponent.Languages.Add(languageId, knowledge);
        }

        EntityManager.Dirty(uidVal, languageComponent);

        var languageSystem2 = _systemManager.GetEntitySystem<LanguageSystem>();
        languageSystem2.UpdateUi(uidVal, languageComponent);

        shell.WriteLine(Loc.GetString("cmd-languageadd-success",
            ("entity", uidVal.ToString()),
            ("language", languageProto.LocalizedName),
            ("knowledge", knowledge)));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 2)
        {
            var languages = _prototypeManager.EnumeratePrototypes<LanguagePrototype>()
                .Select(x => x.ID)
                .ToList();
            var options = new List<string> { "all" };
            options.AddRange(languages);
            return CompletionResult.FromHintOptions(options, Loc.GetString("cmd-language-hint"));
        }

        if (args.Length == 3)
        {
            var knowledgeLevels = Enum.GetNames<LanguageKnowledge>().ToList();
            return CompletionResult.FromHintOptions(knowledgeLevels, Loc.GetString("cmd-knowledge-hint"));
        }

        return CompletionResult.Empty;
    }
}
