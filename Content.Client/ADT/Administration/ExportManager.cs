using Robust.Client.UserInterface;
using System.Text;
using Content.Shared.ADT.Export;


namespace Content.Client.ADT.Export
{
    public sealed class ExportManager : SharedExportManager
    {
        [Dependency] private readonly IFileDialogManager _dialogManager = default!;

        public override async void Load(ExportYmlMessage msg)
        {
            var data = msg.Data;
            var file = await _dialogManager.SaveFile(new FileDialogFilters(new FileDialogFilters.Group("yml")));

            if (file == null)
                return;
            if (file is not { fileStream: var stream })
                return;

            if (data != null)
            {
                await stream.WriteAsync(Encoding.ASCII.GetBytes(data));
                await stream.FlushAsync();
                await stream.DisposeAsync();
                return;
            }
        }
    }
}
