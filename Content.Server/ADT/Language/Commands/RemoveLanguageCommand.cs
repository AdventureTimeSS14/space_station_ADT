using System.Linq;
using Content.Server.Administration;
using Content.Shared.ADT.Language;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Language.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class RemoveLanguageCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntitySystemManager _systemManager = default!;

    public override string Command => "removelanguage";

    public override string Description => Loc.GetString("cmd-removelanguage-desc");

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

        if (!_prototypeManager.TryIndex<LanguagePrototype>(languageId, out var languageProto))
        {
            shell.WriteLine(Loc.GetString("cmd-removelanguage-invalid-language", ("language", languageId)));
            return;
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
            shell.WriteLine(Loc.GetString("cmd-removelanguage-no-language-component", ("entity", uidVal.ToString())));
            return;
        }

        var languageComponent = EntityManager.GetComponent<LanguageSpeakerComponent>(uidVal);

        if (!languageComponent.Languages.ContainsKey(languageId))
        {
            shell.WriteLine(Loc.GetString("cmd-removelanguage-not-known",
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

        var languageSystem = _systemManager.GetEntitySystem<LanguageSystem>();
        languageSystem.UpdateUi(uidVal, languageComponent);

        shell.WriteLine(Loc.GetString("cmd-removelanguage-success",
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
            return CompletionResult.FromHintOptions(languages, "Language");
        }

        return CompletionResult.Empty;
    }
}
