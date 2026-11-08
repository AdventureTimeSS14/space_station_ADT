namespace Content.Shared.ADT.LogicCircuit;

[ImplicitDataDefinitionForInheritors]
public abstract partial class LogicElementBehavior
{
    public virtual void Initialize(LogicElementPrototype prototype)
    {
    }

    public abstract void Process(ref LogicElementContext ctx);
}
