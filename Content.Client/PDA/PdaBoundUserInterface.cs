using Content.Client.CartridgeLoader;
using Content.Shared.CartridgeLoader;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.MedicalScanner;    // ADT-Tweak
using Content.Shared.PDA;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.PDA
{
    [UsedImplicitly]
    public sealed class PdaBoundUserInterface : CartridgeLoaderBoundUserInterface
    {
        private const string MedTekProgramName = "med-tek-program-name"; // ADT-Tweak

        private readonly PdaSystem _pdaSystem;

        [ViewVariables]
        private PdaMenu? _menu;

        private bool _creatingMenu;

        private PdaClientUiStateComponent ClientUiState =>
            EntMan.EnsureComponent<PdaClientUiStateComponent>(Owner);

        public PdaBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
            _pdaSystem = EntMan.System<PdaSystem>();
        }

        protected override void Open()
        {
            base.Open();

            EnsureMenu();
        }

        private void ApplyPreferredView()
        {
            // ADT-Tweak Start - Statements for menu
            if (_menu is null || _menu.Disposed)
                return;
            // ADT-Tweak End

            if (ClientUiState.MedTekSessionActive && ClientUiState.LastHealthScan is { } scan)
            {
                _menu.ShowHealthScan(scan); // ADT-Tweak
                return;
            }

            _menu.RestoreView(ClientUiState.LastView);  // ADT-Tweak
        }

        private void EnsureMenu()
        {
            if (_menu is { Disposed: false })
            {
                ApplyPreferredView();   // ADT-Tweak
                return;
            }

            if (_creatingMenu)
                return;

            _creatingMenu = true;
            try
            {
                CreateMenu();
            }
            finally
            {
                _creatingMenu = false;
            }
        }

        private void CreateMenu()
        {
            _menu = this.CreateWindowCenteredLeft<PdaMenu>();
            _menu.Visible = false;  // ADT-Tweak

            _menu.OnViewChanged += view =>
            {
                if (view != PdaMenu.HealthScanViewIndex)
                    ClientUiState.LastView = view;
            };

            _menu.OnClose += () =>
            {
                if (_menu is { Disposed: false, IsOnHealthScanView: true } &&
                    ClientUiState.LastHealthScan != null)
                    ClientUiState.MedTekSessionActive = true;
            };

            _menu.FlashLightToggleButton.OnToggled += _ =>
            {
                SendMessage(new PdaToggleFlashlightMessage());
            };

            _menu.EjectIdButton.OnPressed += _ =>
            {
                SendPredictedMessage(new ItemSlotButtonPressedEvent(PdaComponent.PdaIdSlotId));
            };

            _menu.EjectPenButton.OnPressed += _ =>
            {
                SendPredictedMessage(new ItemSlotButtonPressedEvent(PdaComponent.PdaPenSlotId));
            };

            _menu.EjectPaiButton.OnPressed += _ =>
            {
                SendPredictedMessage(new ItemSlotButtonPressedEvent(PdaComponent.PdaPaiSlotId));
            };

            _menu.ActivateMusicButton.OnPressed += _ =>
            {
                SendMessage(new PdaShowMusicMessage());
            };

            _menu.AccessRingtoneButton.OnPressed += _ =>
            {
                SendMessage(new PdaShowRingtoneMessage());
            };

            _menu.ShowUplinkButton.OnPressed += _ =>
            {
                SendMessage(new PdaShowUplinkMessage());
            };

            _menu.LockUplinkButton.OnPressed += _ =>
            {
                SendMessage(new PdaLockUplinkMessage());
            };

            _menu.OnProgramItemPressed += OnProgramItemPressed;
            _menu.OnInstallButtonPressed += InstallCartridge;
            _menu.OnUninstallButtonPressed += UninstallCartridge;
            _menu.ProgramCloseButton.OnPressed += _ =>
            {
                EndMedTekSession();
                DeactivateActiveCartridge();
            };

            _menu.OnLeftMedTekView += EndMedTekSession;

            var borderColorComponent = GetBorderColorComponent();
            if (borderColorComponent != null)
            {
                _menu.BorderColor = borderColorComponent.BorderColor;
                _menu.AccentHColor = borderColorComponent.AccentHColor;
                _menu.AccentVColor = borderColorComponent.AccentVColor;
            }

            _menu.ApplyInteriorTheme(
                ResolveSecondaryColor(borderColorComponent),
                ResolveMainFrameColor(borderColorComponent));

            // ADT-Tweak Start - Statements for menu
            ApplyPreferredView();
            _menu.Visible = true;
            // ADT-Tweak End
        }

        private void EndMedTekSession()
        {
            ClientUiState.MedTekSessionActive = false;
        }

        private void OnProgramItemPressed(EntityUid uid)
        {
            if (TryOpenMedTek(uid))
                return;

            EndMedTekSession();
            ActivateCartridge(uid);
        }

        private bool TryOpenMedTek(EntityUid uid)
        {
            if (!EntMan.TryGetComponent(uid, out CartridgeComponent? cartridge))
                return false;

            if (cartridge.ProgramName != MedTekProgramName)
                return false;

            EnsureMenu();
            ClientUiState.MedTekSessionActive = true;
            if (ClientUiState.LastHealthScan is { } scan)
                _menu?.ShowHealthScan(scan);
            return true;
        }

        private void EnterMedTek(HealthAnalyzerUiState state)
        {
            ClientUiState.LastHealthScan = state;
            ClientUiState.MedTekSessionActive = true;
            _menu?.ShowHealthScan(state);
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            base.ReceiveMessage(message);

            if (message is not HealthAnalyzerScannedUserMessage scanMessage)
                return;

            ClientUiState.LastHealthScan = scanMessage.State;

            if (scanMessage.OpenUi)
            {
                ClientUiState.MedTekSessionActive = true; // ADT-Tweak
                EnsureMenu();
                EnterMedTek(scanMessage.State);
                return;
            }

            if (_menu is { Disposed: false, IsOnHealthScanView: true })
                _menu.UpdateHealthScanData(scanMessage.State);
        }

        private static Color ResolveSecondaryColor(PdaBorderColorComponent? border)
        {
            var hex = border?.AccentVColor ?? border?.AccentHColor ?? border?.BorderColor ?? "#B0B0B8";
            return Color.FromHex(hex, Color.FromHex("#B0B0B8"));
        }

        private static Color ResolveMainFrameColor(PdaBorderColorComponent? border)
        {
            return Color.FromHex(border?.BorderColor ?? "#B0B0B8", Color.FromHex("#B0B0B8"));
        }
        // ADT-Tweak end

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            // ADT-Tweak Start: Deprecated MedTek session
            // base.UpdateState(state);

            // if (ClientUiState.MedTekSessionActive && ClientUiState.LastHealthScan is { } scan)
            // {
            //     EnsureMenu();
            //     _menu?.ShowHealthScan(scan);
            // }

            // _menu?.ThemeProgramView();

            // if (state is not PdaUpdateState updateState)
            //     return;
            // ADT-Tweak End
            EnsureMenu();

            if (_menu == null)
            {
                _pdaSystem.Log.Error("PDA state received before menu was created.");
                return;
            }
            
            // ADT-Tweak - New logic for MedTek (WIP)
            var keepMedTek = ClientUiState.MedTekSessionActive && ClientUiState.LastHealthScan != null;
            if (keepMedTek)
                _menu.Visible = false;

            base.UpdateState(state);

            _menu.ThemeProgramView();

            if (state is PdaUpdateState updateState)
                _menu.UpdateState(updateState);

            ApplyPreferredView();
            _menu.Visible = true;
            // ADT-Tweak End
        }

        protected override void AttachCartridgeUI(Control cartridgeUIFragment, string? title)
        {
            _menu?.ProgramView.AddChild(cartridgeUIFragment);

            if (ClientUiState.MedTekSessionActive && ClientUiState.LastHealthScan is { } scan)
            {
                _menu?.ShowHealthScan(scan);
                return;
            }

            EndMedTekSession();
            _menu?.ToProgramView(title ?? Loc.GetString("comp-pda-io-program-fallback-title"));
            _menu?.ThemeProgramView();
        }

        protected override void DetachCartridgeUI(Control cartridgeUIFragment)
        {
            if (_menu is null)
                return;

            _menu.ProgramView.RemoveChild(cartridgeUIFragment);
            
            // ADT-Tweak Start: Deprecated MedTek session
            // if (ClientUiState.MedTekSessionActive && ClientUiState.LastHealthScan is { } scan)
            // {
            //     _menu.ShowHealthScan(scan);
            //     return;
            // }
            // ADT-Tweak End

            if (ClientUiState.MedTekSessionActive && ClientUiState.LastHealthScan != null)
                return;

            _menu.ToHomeScreen();
            _menu.HideProgramHeader();
        }

        protected override void UpdateAvailablePrograms(List<(EntityUid, CartridgeComponent)> programs)
        {
            _menu?.UpdateAvailablePrograms(programs);
        }

        private PdaBorderColorComponent? GetBorderColorComponent()
        {
            return EntMan.GetComponentOrNull<PdaBorderColorComponent>(Owner);
        }
    }
}
