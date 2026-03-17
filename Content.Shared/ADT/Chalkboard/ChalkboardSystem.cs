using Content.Shared.Paper;
using Robust.Shared.Utility;

namespace Content.Shared.ADT.Chalkboard;

public sealed class ChalkboardSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChalkboardComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ChalkboardComponent, MapInitEvent>(OnMapInit, after: new[] { typeof(PaperSystem) });
        SubscribeLocalEvent<ChalkboardComponent, PaperComponent.PaperInputTextMessage>(OnInputText, after: new[] { typeof(PaperSystem) });
    }

    private void OnInit(Entity<ChalkboardComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<MetaDataComponent>(ent, out var meta))
            return;

        if (string.IsNullOrWhiteSpace(ent.Comp.BaseDescription))
            ent.Comp.BaseDescription = meta.EntityDescription;
    }

    private void OnMapInit(Entity<ChalkboardComponent> ent, ref MapInitEvent args)
    {
        UpdateDescription(ent);
    }

    private void OnInputText(Entity<ChalkboardComponent> ent, ref PaperComponent.PaperInputTextMessage args)
    {
        UpdateDescription(ent);
    }

    private void UpdateDescription(Entity<ChalkboardComponent> ent)
    {
        if (!TryComp<PaperComponent>(ent, out var paper))
            return;

        if (!TryComp<MetaDataComponent>(ent, out var meta))
            return;

        var baseDesc = ent.Comp.BaseDescription ?? meta.EntityDescription;
        var content = (paper.Content ?? string.Empty).TrimEnd();

        if (string.IsNullOrWhiteSpace(content))
        {
            _metaData.SetEntityDescription(ent, baseDesc, meta);
            return;
        }

        if (ent.Comp.MaxDescriptionLength > 0 && content.Length > ent.Comp.MaxDescriptionLength)
            content = content[..ent.Comp.MaxDescriptionLength] + "…";

        content = FormattedMessage.EscapeText(content);

        _metaData.SetEntityDescription(ent, $"{baseDesc}\n\n{content}", meta);
    }
}

