using Content.Shared.ADT.Language;

namespace Content.Server.ADT.Shadowling;

[RegisterComponent, Access(typeof(ADTShadowlingAbilitySystem))]
public sealed partial class ADTShadowlingShadowFormComponent : Component
{
    [ViewVariables]
    public EntityUid? RelativeEntity;

    [ViewVariables]
    public Angle RelativeRotation;

    [ViewVariables]
    public Angle TargetRelativeRotation;

    [ViewVariables]
    public Dictionary<string, LanguageKnowledge>? Languages;

    [ViewVariables]
    public string? CurrentLanguage;
}
