using Content.Server.Administration;
using Content.Shared.Emoting;
using Content.Shared.Administration;
using Content.Shared.Speech;
using Robust.Shared.Console;
using Content.Shared.ADT.Language;
using Content.Shared.Mind;

namespace Content.Server.Mind.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class MakeSentientCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly IEntityManager _entManager = default!; // ADT-Tweak

    public override string Command => "makesentient";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("shell-need-exactly-one-argument"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var entNet) || !EntityManager.TryGetEntity(entNet, out var entId) || !EntityManager.EntityExists(entId))
        {
            shell.WriteLine(Loc.GetString("shell-could-not-find-entity-with-uid", ("uid", args[0])));
            return;
        }

        MakeSentient(entId.Value, _entManager, _mindSystem, true, entId); // ADT-Tweak
    }

    // ADT-Tweak-Start
    public static void MakeSentient(EntityUid uid, IEntityManager entityManager, SharedMindSystem mindSystem, bool allowSpeech = true, EntityUid? entId = null)
    {
        if (allowSpeech)
        {
            entityManager.EnsureComponent<SpeechComponent>(uid);
            entityManager.EnsureComponent<EmotingComponent>(uid);
            var lang = entityManager.EnsureComponent<LanguageSpeakerComponent>(uid);
            if (!lang.Languages.ContainsKey("GalacticCommon"))
                lang.Languages.Add("GalacticCommon", LanguageKnowledge.Speak);
            else
                lang.Languages["GalacticCommon"] = LanguageKnowledge.Speak;
        }

        if (!entityManager.EntityExists(entId))
        {
            return;
        }

        mindSystem.MakeSentient(entId.Value);
    }
    // ADT-Tweak-End
}
