using Robust.Client.UserInterface;
using Robust.Client.UserInterface.XAML;

namespace Content.Client.ADT.Hands.UI
{
    public sealed class HandPlaceholderStatus : Control
    {
        public HandPlaceholderStatus()
        {
            RobustXamlLoader.Load(this);
        }
    }
}
