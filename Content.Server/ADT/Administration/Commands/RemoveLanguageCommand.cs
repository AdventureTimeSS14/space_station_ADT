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
public sealed class RemoveLanguageCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntitySystemManager _systemManager = default!;

    public override string Command => "languageremove";

    public override string Description => Loc.GetString("cmd-languageremove-desc");

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

        var nent = new NetEntity(entInt);

        if (!EntityManager.TryGetEntity(nent, out var uid))
        {
            shell.WriteLine(Loc.GetString("shell-invalid-entity-id"));
            return;
        }

        var uidVal = uid.Value;

        if (!EntityManager.HasComponent<LanguageSpeakerComponent>(uidVal))
        {
            shell.WriteLine(Loc.GetString("cmd-languageremove-no-language-component", ("entity", uidVal.ToString())));
            return;
        }

        var languageComponent = EntityManager.GetComponent<LanguageSpeakerComponent>(uidVal);

        // Удаление всех языков
        if (languageId.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var removedCount = languageComponent.Languages.Count;
            languageComponent.Languages.Clear();
            languageComponent.CurrentLanguage = "Universal";

            EntityManager.Dirty(uidVal, languageComponent);

            var languageSystem = _systemManager.GetEntitySystem<LanguageSystem>();
            languageSystem.UpdateUi(uidVal, languageComponent);

            shell.WriteLine(Loc.GetString("cmd-languageremove-all-success",
                ("entity", uidVal.ToString()),
                ("count", removedCount)));
            return;
        }

        if (!_prototypeManager.TryIndex<LanguagePrototype>(languageId, out var languageProto))
        {
            shell.WriteLine(Loc.GetString("cmd-languageremove-invalid-language", ("language", languageId)));
            return;
        }

        if (!languageComponent.Languages.ContainsKey(languageId))
        {
            shell.WriteLine(Loc.GetString("cmd-languageremove-not-known",
                ("entity", uidVal.ToString()),
                ("language", languageProto.LocalizedName)));
            return;
        }

        languageComponent.Languages.Remove(languageId);

        if (languageComponent.CurrentLanguage == languageId)
        {
            languageComponent.CurrentLanguage = languageComponent.Languages.Keys.FirstOrDefault("Universal");
        }

        EntityManager.Dirty(uidVal, languageComponent);

        var languageSystem2 = _systemManager.GetEntitySystem<LanguageSystem>();
        languageSystem2.UpdateUi(uidVal, languageComponent);

        shell.WriteLine(Loc.GetString("cmd-languageremove-success",
            ("entity", uidVal.ToString()),
            ("language", languageProto.LocalizedName)));
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

        return CompletionResult.Empty;
    }
}
