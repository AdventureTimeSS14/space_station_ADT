using Content.Server.Inventory;
using Content.Shared.Actions;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.ADT.Silicons.Borgs.Components;
using Content.Shared.ADT.TTS;
using Content.Shared.Interaction.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Server.Silicons.Borgs;

/// <summary>
/// Server-side logic for borg type switching. Handles more heavyweight and server-specific switching logic.
/// </summary>
public sealed partial class BorgSwitchableTypeSystem : SharedBorgSwitchableTypeSystem
{
    [Dependency] private BorgSystem _borgSystem = default!;
    [Dependency] private ServerInventorySystem _inventorySystem = default!;
    // ADT-Tweak start
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedActionsSystem _actionsSystem = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private InteractionPopupSystem _interactionPopup = default!;

    private static readonly ResPath BaseChassisRsi = new("Mobs/Silicon/chassis.rsi");
    private const string BaseChassisSpriteState = "robot";
    private const string BaseChassisTransponderName = "cyborg";
    private const string BaseChassisInventoryTemplate = "borg";
    // ADT-Tweak end

    protected override void SelectBorgModule(Entity<BorgSwitchableTypeComponent> ent, ProtoId<BorgTypePrototype> borgType)
    {
        var prototype = ProtoMan.Index(borgType);

        // Assign radio channels
        string[] radioChannels = [.. ent.Comp.InherentRadioChannels, .. prototype.RadioChannels];
        if (TryComp(ent, out IntrinsicRadioTransmitterComponent? transmitter))
            transmitter.Channels = [.. radioChannels];

        if (TryComp(ent, out ActiveRadioComponent? activeRadio))
            activeRadio.Channels = [.. radioChannels];

        // ADT-Tweak-AiRemoteControl-Start
        if (TryComp(ent, out AiRemoteControllerComponent? aiRemoteComp))
        {
            if (TryComp(aiRemoteComp.AiHolder, out IntrinsicRadioTransmitterComponent? stationAiTransmitter) && transmitter != null)
            {
                aiRemoteComp.PreviouslyTransmitterChannels = [.. radioChannels, ];
                transmitter.Channels = [.. stationAiTransmitter.Channels, ];
            }

            if (TryComp(aiRemoteComp.AiHolder, out ActiveRadioComponent? stationAiActiveRadio) && activeRadio != null)
            {
                aiRemoteComp.PreviouslyActiveRadioChannels = [.. radioChannels, ];
                activeRadio.Channels = [.. stationAiActiveRadio.Channels, ];
            }
        }
        // ADT-Tweak-AiRemoteControl-End

        // Borg transponder for the robotics console
        if (TryComp(ent, out BorgTransponderComponent? transponder))
        {
            _borgSystem.SetTransponderSprite(
                (ent.Owner, transponder),
                new SpriteSpecifier.Rsi(new ResPath("Mobs/Silicon/chassis.rsi"), prototype.SpriteBodyState));

            _borgSystem.SetTransponderName(
                (ent.Owner, transponder),
                Loc.GetString($"borg-type-{borgType}-transponder"));
        }

        // Configure modules
        if (TryComp(ent, out BorgChassisComponent? chassis))
        {
            var chassisEnt = (ent.Owner, chassis);
            _borgSystem.SetMaxModules(
                chassisEnt,
                prototype.ExtraModuleCount + prototype.DefaultModules.Length);

            _borgSystem.SetModuleWhitelist(chassisEnt, prototype.ModuleWhitelist);

            foreach (var module in prototype.DefaultModules)
            {
                var moduleEntity = Spawn(module);
                var borgModule = Comp<BorgModuleComponent>(moduleEntity);
                _borgSystem.SetBorgModuleDefault((moduleEntity, borgModule), !HasComp<ADTBorgStorageModuleComponent>(moduleEntity)); // ADT-Tweak
                _borgSystem.InsertModule(chassisEnt, moduleEntity);
            }
        }

        // Configure special components
        if (ProtoMan.Resolve(ent.Comp.SelectedBorgType, out var previousPrototype))
        {
            if (previousPrototype.AddComponents is { } removeComponents)
                EntityManager.RemoveComponents(ent, removeComponents);
        }

        if (prototype.AddComponents is { } addComponents)
        {
            EntityManager.AddComponents(ent, addComponents);
        }

        // Configure inventory template (used for hat spacing)
        if (TryComp(ent, out InventoryComponent? inventory))
        {
            _inventorySystem.SetTemplateId((ent.Owner, inventory), prototype.InventoryTemplateId);
        }

        base.SelectBorgModule(ent, borgType);
    }

    // ADT-Tweak start
    public bool TryResetBorgType(Entity<BorgSwitchableTypeComponent> ent, EntityUid? user = null)
    {
        if (ent.Comp.SelectedBorgType is not { } selected || !ProtoMan.Resolve(selected, out var previousPrototype))
            return false;

        if (TryComp(ent, out BorgChassisComponent? chassis))
        {
            var chassisEnt = (ent.Owner, chassis);
            _container.EmptyContainer(chassis.ModuleContainer);
            _borgSystem.SetMaxModules(chassisEnt, 0);
            _borgSystem.SetModuleWhitelist(chassisEnt, null);
        }

        if (previousPrototype.AddComponents is { } removeComponents)
            EntityManager.RemoveComponents(ent, removeComponents);

        ResetBorgAppearance(ent);

        if (TryComp(ent, out AiRemoteControllerComponent? aiRemoteComp) && aiRemoteComp.AiHolder != null)
        {
            if (TryComp(aiRemoteComp.AiHolder, out IntrinsicRadioTransmitterComponent? aiTransmitter)
                && TryComp(ent, out IntrinsicRadioTransmitterComponent? transmitter))
            {
                aiRemoteComp.PreviouslyTransmitterChannels = [.. ent.Comp.InherentRadioChannels];
                transmitter.Channels = [.. aiTransmitter.Channels];
            }

            if (TryComp(aiRemoteComp.AiHolder, out ActiveRadioComponent? aiRadio)
                && TryComp(ent, out ActiveRadioComponent? activeRadio))
            {
                aiRemoteComp.PreviouslyActiveRadioChannels = [.. ent.Comp.InherentRadioChannels];
                activeRadio.Channels = [.. aiRadio.Channels];
            }
        }
        else
        {
            if (TryComp(ent, out IntrinsicRadioTransmitterComponent? transmitter))
                transmitter.Channels = [.. ent.Comp.InherentRadioChannels];

            if (TryComp(ent, out ActiveRadioComponent? activeRadio))
                activeRadio.Channels = [.. ent.Comp.InherentRadioChannels];
        }

        if (TryComp(ent, out BorgTransponderComponent? transponder))
        {
            _borgSystem.SetTransponderSprite(
                (ent.Owner, transponder),
                new SpriteSpecifier.Rsi(BaseChassisRsi, BaseChassisSpriteState));

            _borgSystem.SetTransponderName((ent.Owner, transponder), BaseChassisTransponderName);
        }

        if (TryComp(ent, out InventoryComponent? inventory))
            _inventorySystem.SetTemplateId((ent.Owner, inventory), BaseChassisInventoryTemplate);

        RemComp<SpriteMovementComponent>(ent);

        if (TryComp(ent, out BorgSwitchableSubtypeComponent? subtypeComp))
        {
            subtypeComp.BorgSubtype = null;
            Dirty(ent, subtypeComp);
        }

        ent.Comp.SelectedBorgType = null;
        _actionsSystem.AddAction(ent, ref ent.Comp.SelectTypeAction, ActionId);
        Dirty(ent);

        _popup.PopupEntity(Loc.GetString("adt-borg-wires-popup-reset-module"), ent.Owner, user ?? ent.Owner);

        return true;
    }

    private void ResetBorgAppearance(Entity<BorgSwitchableTypeComponent> ent)
    {
        var proto = MetaData(ent).EntityPrototype;
        if (proto == null)
            return;

        if (TryComp(ent, out InteractionPopupComponent? popup)
            && proto.Components.TryGetValue("InteractionPopup", out var popupReg)
            && popupReg.Component is InteractionPopupComponent popupBase)
        {
            if (popupBase.InteractSuccessString is { } successString)
                _interactionPopup.SetInteractSuccessString((ent.Owner, popup), successString);

            if (popupBase.InteractFailureString is { } failureString)
                _interactionPopup.SetInteractFailureString((ent.Owner, popup), failureString);
        }

        if (TryComp(ent, out FootstepModifierComponent? footstep)
            && proto.Components.TryGetValue("FootstepModifier", out var footstepReg)
            && footstepReg.Component is FootstepModifierComponent footstepBase)
        {
            footstep.FootstepSoundCollection = footstepBase.FootstepSoundCollection;
            Dirty(ent, footstep);
        }

        if (TryComp(ent, out TTSComponent? tts)
            && proto.Components.TryGetValue("TTS", out var ttsReg)
            && ttsReg.Component is TTSComponent ttsBase)
        {
            tts.VoicePrototypeId = ttsBase.VoicePrototypeId;
            Dirty(ent, tts);
        }
    }
    // ADT-Tweak end
}
