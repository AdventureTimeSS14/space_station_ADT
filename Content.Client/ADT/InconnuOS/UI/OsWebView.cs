using Robust.Client.UserInterface;
using Robust.Client.WebView;

namespace Content.Client.ADT.InconnuOS.UI;

// WebViewControl.ExitedTree closes the browser without checking IsOpen, which asserts when
// WebViewManager.Shutdown already closed it. AlwaysActive skips that path; the setter does check IsOpen.
public sealed class OsWebView : Control
{
    public readonly WebViewControl Web;

    public OsWebView()
    {
        Web = new WebViewControl
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            AlwaysActive = true,
        };

        AddChild(Web);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            RemoveChild(Web);
            Web.AlwaysActive = false;
            Web.Dispose();
        }

        base.Dispose(disposing);
    }
}
