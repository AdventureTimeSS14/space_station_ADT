using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Shared.ADT.Language;

namespace Content.Server.Mind.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed class MakeSentientCommand : LocalizedEntityCommands
{
    [Dependency] private readonly MindSystem _mindSystem = default!;

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
            entityManager.EnsureComponent<MindContainerComponent>(uid);
            if (allowMovement)
            {
                entityManager.EnsureComponent<InputMoverComponent>(uid);
                entityManager.EnsureComponent<MobMoverComponent>(uid);
                entityManager.EnsureComponent<MovementSpeedModifierComponent>(uid);
            }

            if (allowSpeech)
            {
                entityManager.EnsureComponent<SpeechComponent>(uid);
                entityManager.EnsureComponent<EmotingComponent>(uid);

                // ADT Languages start
                var lang = entityManager.EnsureComponent<LanguageSpeakerComponent>(uid);
                if (!lang.Languages.ContainsKey("GalacticCommon"))
                    lang.Languages.Add("GalacticCommon", LanguageKnowledge.Speak);
                else
                    lang.Languages["GalacticCommon"] = LanguageKnowledge.Speak;
                // ADT Langusges end
            }

            entityManager.EnsureComponent<ExaminerComponent>(uid);
        }

        _mindSystem.MakeSentient(entId.Value);
    }
}
