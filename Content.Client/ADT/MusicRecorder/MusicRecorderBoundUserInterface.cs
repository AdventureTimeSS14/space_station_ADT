using System.IO;
using Content.Shared.ADT.MusicRecorder;
using Content.Shared.Containers.ItemSlots;
using Robust.Client.Audio;
using Robust.Client.UserInterface;

namespace Content.Client.ADT.MusicRecorder;

public sealed class MusicRecorderBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IAudioManager _audio = default!;
    [Dependency] private readonly IFileDialogManager _dialogs = default!;

    private MusicRecorderWindow? _window;
    private bool _dialogOpen;
    private bool _uploading;
    private int _nextTransfer = 1;

    public MusicRecorderBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<MusicRecorderWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;
        _window.OnSlotPressed += OnSlotPressed;
        _window.OnRename += OnRename;
        _window.OnUpload += OnUpload;
        _window.OnRemoveTrack += OnRemoveTrack;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not MusicRecorderBoundUserInterfaceState castState)
            return;

        _window?.UpdateState(castState);
    }

    private void OnSlotPressed()
    {
        SendMessage(new ItemSlotButtonPressedEvent(MusicRecorderComponent.CassetteSlotId));
    }

    private void OnRename(string name)
    {
        SendMessage(new MusicRecorderRenameMessage(name));
    }

    private void OnRemoveTrack(string trackId)
    {
        SendMessage(new MusicRecorderRemoveTrackMessage(trackId));
    }

    private async void OnUpload()
    {
        if (_dialogOpen || _uploading || _window == null)
            return;

        _dialogOpen = true;
        Stream? file = null;

        try
        {
            file = await _dialogs.OpenFile(new FileDialogFilters(new FileDialogFilters.Group("ogg")), FileAccess.Read);
            if (file == null || _window.Disposed)
                return;

            using var memory = new MemoryStream();
            var buffer = new byte[81920];
            int read;
            while ((read = file.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (memory.Length + read > MusicCassetteComponent.MaxTrackBytes)
                {
                    _window.SetStatus(Loc.GetString("music-recorder-too-large"), error: true);
                    return;
                }

                memory.Write(buffer, 0, read);
            }

            var bytes = memory.ToArray();
            if (bytes.Length == 0 || !MusicOgg.TryGetChannelCount(bytes, out var channels))
            {
                _window.SetStatus(Loc.GetString("music-recorder-not-ogg"), error: true);
                return;
            }

            if (channels != 1)
            {
                _window.SetStatus(Loc.GetString("music-recorder-not-mono"), error: true);
                return;
            }

            AudioStream decoded;
            try
            {
                decoded = _audio.LoadAudioOggVorbis(new MemoryStream(bytes), "music-recorder-upload");
            }
            catch (Exception)
            {
                _window.SetStatus(Loc.GetString("music-recorder-not-ogg"), error: true);
                return;
            }

            using (decoded)
            {
                if (decoded.ChannelCount != 1)
                {
                    _window.SetStatus(Loc.GetString("music-recorder-not-mono"), error: true);
                    return;
                }

                var duration = (float) decoded.Length.TotalSeconds;
                if (duration < 0.5f || duration > MusicCassetteComponent.MaxDurationSeconds)
                {
                    _window.SetStatus(Loc.GetString("music-recorder-too-long"), error: true);
                    return;
                }

                SendUpload(bytes, duration, NextTrackName());
            }
        }
        catch (Exception)
        {
            _window?.SetStatus(Loc.GetString("music-recorder-not-ogg"), error: true);
        }
        finally
        {
            file?.Dispose();
            _dialogOpen = false;
        }
    }

    private string NextTrackName()
    {
        var number = 1;
        if (State is MusicRecorderBoundUserInterfaceState recorderState)
            number = recorderState.Tracks.Length + 1;

        return Loc.GetString("music-recorder-default-track", ("number", number));
    }

    private void SendUpload(byte[] bytes, float duration, string name)
    {
        if (_window == null || _window.Disposed)
            return;

        _uploading = true;
        _window.SetUploading(true);

        var transferId = _nextTransfer++;
        SendMessage(new MusicRecorderUploadHeaderMessage(transferId, bytes.Length, duration, name));

        for (var offset = 0; offset < bytes.Length; offset += MusicCassetteComponent.ChunkBytes)
        {
            var count = Math.Min(MusicCassetteComponent.ChunkBytes, bytes.Length - offset);
            var chunk = new byte[count];
            Array.Copy(bytes, offset, chunk, 0, count);
            SendMessage(new MusicRecorderUploadChunkMessage(transferId, offset, chunk));

            var percent = (offset + count) * 100 / bytes.Length;
            _window.SetStatus(Loc.GetString("music-recorder-uploading", ("percent", percent)));
        }

        SendMessage(new MusicRecorderUploadFinishMessage(transferId));
        _uploading = false;
        _window.SetUploading(false);
        _window.SetStatus(Loc.GetString("music-recorder-upload-sent"));
    }
}
