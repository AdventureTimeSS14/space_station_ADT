using Content.Shared.ADT.TTS;

namespace Content.Shared.ADT.Traits.Effects;

public sealed partial class SetTTSEffect : BaseTraitEffect
{
    [DataField(required: true)]
    public string Effect = string.Empty;

    public override void Apply(TraitEffectContext ctx)
    {
        if (!ctx.EntMan.TryGetComponent<TTSComponent>(ctx.Player, out var tts))
            return;

        tts.Effect = Effect;
    }
}
