using System.Linq;
using Content.Server.Administration;
using Content.Server.ADT.Disease;
using Content.Server.Corvax.TTS;
using Content.Server.Humanoid;
using Content.Shared.Administration;
using Content.Shared.ADT.Disease;
using Content.Shared.Corvax.TTS;
using Content.Shared.Humanoid;
using Robust.Shared.Toolshed;
using Robust.Shared.Toolshed.TypeParsers;

namespace Content.Server.ADT.Administration.Commands.Toolshed;

[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed class ChangeTTSCommand : ToolshedCommand
{
    private HumanoidAppearanceSystem? _appearanceSystem;

    [CommandImplementation]
    public EntityUid? ChangeTTS(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] EntityUid input,
        [CommandArgument] Prototype<TTSVoicePrototype> prototype
    )
    {
        if (!EntityManager.TryGetComponent<HumanoidAppearanceComponent>(input, out var humanoidAppearanceComponent))
        {
            ctx.ReportError(new NotHumanoidError());
            return null;
        }

        _appearanceSystem ??= GetSys<HumanoidAppearanceSystem>();

        _appearanceSystem.SetTTSVoice(input, prototype.Value.ID, humanoid: humanoidAppearanceComponent);
        return input;
    }

    [CommandImplementation]
    public IEnumerable<EntityUid> ChangeTTS(
        [CommandInvocationContext] IInvocationContext ctx,
        [PipedArgument] IEnumerable<EntityUid> input,
        [CommandArgument] Prototype<TTSVoicePrototype> prototype
    )
        => input.Select(x => ChangeTTS(ctx, x, prototype)).Where(x => x is not null).Select(x => (EntityUid) x!);
}
