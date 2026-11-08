using Content.Shared.ADT.Language;

namespace Content.Client.ADT.Language;

public sealed partial class LanguageSystem : SharedLanguageSystem
{
    public event Action<LanguageMenuStateMessage>? OnMenuStateReceived;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<LanguageMenuStateMessage>(OnMenuState);
    }

    private void OnMenuState(LanguageMenuStateMessage ev)
    {
        OnMenuStateReceived?.Invoke(ev);
    }
}
