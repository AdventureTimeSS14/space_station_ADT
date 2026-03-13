using System.Linq;
using System.Text;
using Content.Server.Administration;
using Content.Shared.ADT.Language;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.ADT.Language.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class ListLanguagesCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override string Command => "listlanguages";

    public override string Description => Loc.GetString("cmd-listlanguages-desc");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!int.TryParse(args[0], out var entInt))
        {
            shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
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
            shell.WriteLine(Loc.GetString("cmd-listlanguages-no-language-component", ("entity", uidVal.ToString())));
            return;
        }

        var languageComponent = EntityManager.GetComponent<LanguageSpeakerComponent>(uidVal);

        var builder = new StringBuilder();
        builder.AppendLine(Loc.GetString("cmd-listlanguages-header", ("entity", uidVal.ToString())));

        var currentLanguage = languageComponent.CurrentLanguage;
        
        foreach (var (langId, knowledge) in languageComponent.Languages)
        {
            var langProto = _prototypeManager.TryIndex<LanguagePrototype>(langId, out var proto) 
                ? proto.LocalizedName 
                : langId;
            var isCurrent = langId == currentLanguage ? " [CURRENT]" : "";
            builder.AppendLine($"  - {langProto} ({langId}): {knowledge}{isCurrent}");
        }

        if (languageComponent.Languages.Count == 0)
        {
            builder.AppendLine(Loc.GetString("cmd-listlanguages-empty"));
        }

        shell.WriteLine(builder.ToString());
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return CompletionResult.Empty;
    }
}
