using System.Text.RegularExpressions;

namespace Content.Shared.ADT.LogicCircuit;

public enum LogicCircuitValidationError : byte
{
    None = 0,
    TooManyNodes,
    TooManyWires,
    EmptyNodeId,
    NodeIdTooLong,
    DuplicateNodeId,
    UnknownElement,
    UnknownWireNode,
    UnknownPin,
    InputAlreadyWired,
    ConfigTooLong,
    NotEnoughPower,
}

public static class LogicCircuitErrors
{
    public static string GetMessage(LogicCircuitValidationError error, string detail)
    {
        var name = Enum.GetName(error) ?? "unknown";
        var key = Regex.Replace(name, "(?<!^)([A-Z])", "-$1").ToLowerInvariant();

        return Loc.GetString($"logic-circuit-error-{key}", ("detail", detail));
    }
}
