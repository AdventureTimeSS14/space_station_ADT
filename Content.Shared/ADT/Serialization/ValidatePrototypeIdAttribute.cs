using Robust.Shared.Prototypes;

namespace Robust.Shared.Analyzers;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ValidatePrototypeIdAttribute<T> : Attribute where T : class, IPrototype;
