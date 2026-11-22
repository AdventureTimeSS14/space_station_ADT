using Content.Shared.ADT.MedicalAssembler;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.MedicalAssembler;

[UsedImplicitly]
public sealed class MedicalAssemblerUiBoundUserInterface : BoundUserInterface
{
    private MedicalAssemblerMenu? _menu;

    public MedicalAssemblerUiBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<MedicalAssemblerMenu>();
        _menu.OnCraftPressed += recipeId => SendMessage(new MedicalAssemblerCraftMessage(recipeId));
        _menu.OnEjectPressed += () => SendMessage(new MedicalAssemblerEjectMaterialsMessage());
        _menu.OnCreateCustomPressed += message => SendMessage(message);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is MedicalAssemblerUpdateState cast && _menu != null)
            _menu.UpdateState(cast);
    }
}
