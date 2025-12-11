using Content.Shared.ADT.MidroundCustomization;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.MidroundCustomization;

public sealed class MidroundCustomizationBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private MidroundCustomizationWindow? _window;

    public MidroundCustomizationBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<MidroundCustomizationWindow>();

        _window.OnSlotMarkingSelected += args => SendMessage(new MidroundCustomizationMarkingSelectMessage(args.Category, args.Id, args.Slot));
        _window.OnSlotColorChanged += args => SendMessage(new MidroundCustomizationChangeColorMessage(args.Category, args.Colors, args.Slot));
        _window.OnSlotAdded += args => SendMessage(new MidroundCustomizationAddSlotMessage(args));
        _window.OnSlotRemoved += args => SendMessage(new MidroundCustomizationRemoveSlotMessage(args.Category, args.Slot));

        _window.OnVoiceChanged += voiceId => SendMessage(new MidroundCustomizationChangeVoiceMessage(voiceId, voiceId));
        _window.OnBarkProtoChanged += proto => SendMessage(new MidroundCustomizationChangeBarkProtoMessage(proto));
        _window.OnBarkPitchChanged += pitch => SendMessage(new MidroundCustomizationChangeBarkPitchMessage(pitch));
        _window.OnBarkMinVarChanged += minVar => SendMessage(new MidroundCustomizationChangeBarkMinVarMessage(minVar));
        _window.OnBarkMaxVarChanged += maxVar => SendMessage(new MidroundCustomizationChangeBarkMaxVarMessage(maxVar));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not MidroundCustomizationUiState data || _window == null)
        {
            return;
        }

        _window.UpdateState(data);
    }
}
