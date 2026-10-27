using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client.ADT.UserData;

public static class ADTUserData
{
    public static readonly ResPath Root = new("/ADT");

    public static void MoveLegacy(IWritableDirProvider data, ResPath legacy, ResPath path)
    {
        if (data.Exists(path) || !data.Exists(legacy))
            return;

        data.CreateDir(path.Directory);
        data.Rename(legacy, path);
    }
}
