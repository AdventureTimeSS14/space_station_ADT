using Content.Server.SD.Cardreader.Components;
using Content.Shared.SD.Cardreader.Components;
using Content.Server.DeviceLinking.Systems;
using Robust.Server.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Timing;
using Content.Shared.Item;

namespace Content.Server.SD.Cardreader.Systems;

public sealed class CardreaderSystem : EntitySystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IGameTiming _timing = default!;


    public override void Initialize()
    {
        SubscribeLocalEvent<CardreaderComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void DeleteCardSafely(EntityUid card)
    {
        if (!Exists(card))
            return;

        RemComp<RfidComponent>(card);

        Timer.Spawn(0, () =>
        {
            if (Exists(card))
                QueueDel(card);
        });
    }

    private void OnInteractUsing(EntityUid uid, CardreaderComponent component, ref InteractUsingEvent args)
    {
        // Помечаем событие как обработанное СРАЗУ, чтобы другие системы его игнорировали
        if (args.Handled)
            return;
        args.Handled = true;

        var time = _timing.CurTime;
        if ((time - component.LastActivationTime).TotalSeconds < component.Cooldown)
        {
            return;
        }

        // Проверяем, что это RFID-карта
        if (!TryComp<RfidComponent>(args.Used, out var rfid))
        {
            return;
        }

        // Проверяем ограниченное использование
        if (rfid.LimitedUse && rfid.UsesLeft <= 0)
        {
            _audio.PlayPvs(component.SoundDeny, uid);
            Logger.Debug($"Card depleted! UID={args.Used}");
            return;
        }

        // Проверяем уровень доступа
        bool accessGranted = component.AllowHigherAccess
            ? rfid.AccessLevel >= component.RequiredAccess
            : rfid.AccessLevel == component.RequiredAccess;

        if (accessGranted)
        {
            // Устанавливаем кулдаун ПЕРЕД отправкой сигнала
            component.LastActivationTime = time;

            // Сначала выполняем все действия с картридером
            _signalSystem.InvokePort(uid, component.Trigger);
            _audio.PlayPvs(component.SoundAccept, uid);
            Logger.Debug($"Cardreader accepted! Handled={args.Handled}, UID={uid}, User={args.User}");

            // Затем обрабатываем ограничение использований
            if (rfid.LimitedUse)
            {
                rfid.UsesLeft--;

                if (rfid.UsesLeft <= 0)
                {
                    DeleteCardSafely(args.Used);
                    Logger.Debug($"Card expired after use! UID={args.Used}");
                }
                else
                {
                    Dirty(args.Used, rfid);
                }
            }
        }
        else
            {
                _audio.PlayPvs(component.SoundDeny, uid);
                Logger.Debug($"Cardreader declined! Handled={args.Handled}, UID={uid}, User={args.User}");
            }
    }
}
