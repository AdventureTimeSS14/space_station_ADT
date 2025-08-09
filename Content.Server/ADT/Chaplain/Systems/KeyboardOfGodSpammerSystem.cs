using Content.Server.Chat.Systems;
using Content.Shared.Chaplain.Components;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;

namespace Content.Server.Chaplain.EntitySystems;

public sealed class KeyboardOfGodSpammerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    private const string KeyboardSymbols = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()_+-=[]{}\\|;:'\",.<>/?`~";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<KeyboardOfGodSpammerComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMeleeHit(EntityUid uid, KeyboardOfGodSpammerComponent component, MeleeHitEvent args)
    {
        var length = _random.Next(5, 16);
        var message = GenerateRandomString(length);
        _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, true);
    }

    private string GenerateRandomString(int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = _random.Pick(KeyboardSymbols.ToCharArray());
        }
        return new string(chars);
    }
}