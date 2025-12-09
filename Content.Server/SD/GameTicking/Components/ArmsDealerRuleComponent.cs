using Content.Shared.Random;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Content.Server._SD.GameTicking.Rules;

namespace Content.Server._SD.GameTicking.Components;

[RegisterComponent, Access(typeof(ArmsDealerRuleSystem))]
public sealed partial class ArmsDealerRuleComponent : Component;
