using Content.Shared.Audio.Jukebox;
using Robust.Client.Audio;
using Robust.Client.UserInterface;
using Robust.Shared.Audio.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Content.Shared.ADT.Audio.Jukebox;

namespace Content.Client.Audio.Jukebox;

public sealed class JukeboxBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    [ViewVariables]
    private JukeboxMenu? _menu;

    public JukeboxBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        //IoCManager.InjectDependencies(this); ADT-Tweak
    }

    protected override void Open()
    {
        base.Open();
        _menu = this.CreateWindow<JukeboxMenu>();

        _menu.OnPlayPressed += args =>
        {
            if (args)
            {
                SendMessage(new JukeboxPlayingMessage());
            }
            else
            {
                SendMessage(new JukeboxPauseMessage());
            }
        };

        _menu.OnStopPressed += () =>
        {
            SendMessage(new JukeboxStopMessage());
        };

        // ADT-Tweak start
        _menu.OnLoopToggled += () =>
        {
            SendMessage(new JukeboxToggleLoopMessage());
        };
        _menu.OnEjectPressed += () =>
        {
            SendMessage(new JukeboxEjectMessage());
        };
        // ADT-Tweak end

        _menu.OnSongSelected += SelectSong;
        _menu.SetTime += SetTime;
        _menu.SetVolume += SetVolume; // ADT-Tweak
        PopulateMusic();
        Reload();
    }

    /// <summary>
    /// Reloads the attached menu if it exists.
    /// </summary>
    public void Reload()
    {
        if (_menu == null || !EntMan.TryGetComponent(Owner, out JukeboxComponent? jukebox))
            return;

        _menu.SetAudioStream(jukebox.AudioStream);
        _menu.SetVolumeSlider(jukebox.Volume); // ADT-Tweak
        _menu.SetLoopButton(jukebox.LoopEnabled); // ADT-Tweak
        if (_protoManager.Resolve(jukebox.SelectedSongId, out var songProto))
        {
            var length = EntMan.System<AudioSystem>().GetAudioLength(songProto.Path.Path.ToString());
            _menu.SetSelectedSong(songProto.Name, (float)length.TotalSeconds); // ADT-Tweak
        }
        else
        {
            _menu.SetSelectedSong(string.Empty, 0f);
        }

        // ADT-Tweak start: Update disk name and eject button state
        UpdateDiskInfo();
        // ADT-Tweak end
    }

    public void PopulateMusic()
    {
        //ADT-Tweak-Start
        if (_menu == null)
            return;

        var jukeboxEntity = (Owner, EntMan.GetComponent<JukeboxComponent>(Owner));
        var availableSongs = new List<JukeboxPrototype>();

        foreach (var songId in EntMan.System<SharedJukeboxSystem>().GetAvailableSongs(jukeboxEntity))
        {
            if (_protoManager.Resolve(songId, out JukeboxPrototype? songProto))
            {
                availableSongs.Add(songProto);
            }
        }

        _menu.Populate(availableSongs);
        //_menu?.Populate(_protoManager.EnumeratePrototypes<JukeboxPrototype>());
        //ADT-Tweak-End
    }

    public void SelectSong(ProtoId<JukeboxPrototype> songid)
    {
        SendMessage(new JukeboxSelectedMessage(songid));
    }

    public void SetTime(float time)
    {
        var sentTime = time;

        // You may be wondering, what the fuck is this
        // Well we want to be able to predict the playback slider change, of which there are many ways to do it
        // We can't just use SendPredictedMessage because it will reset every tick and audio updates every frame
        // so it will go BRRRRT
        // Using ping gets us close enough that it SHOULD, MOST OF THE TIME, fall within the 0.1 second tolerance
        // that's still on engine so our playback position never gets corrected.
        if (EntMan.TryGetComponent(Owner, out JukeboxComponent? jukebox) &&
            EntMan.TryGetComponent(jukebox.AudioStream, out AudioComponent? audioComp))
        {
            audioComp.PlaybackPosition = time;
        }

        SendMessage(new JukeboxSetTimeMessage(sentTime));
    }

    /// ADT-Tweak start
    /// First applies the volume locally for prediction (if components are available),
    /// then sends a message to the server for synchronization.
    /// Uses MapToRange to convert the slider value to the actual audio component volume range.
    /// </summary>
    /// <param name="volume">Volume value from the UI slider (typically from 0 to 1).</param>

    public void SetVolume(float volume)
    {
        var sentVolume = volume;

        // Prediction
        if (EntMan.TryGetComponent(Owner, out JukeboxComponent? jukebox) &&
            EntMan.TryGetComponent(jukebox.AudioStream, out AudioComponent? audioComp))
        {
            audioComp.Volume = SharedJukeboxSystem.MapToRange(volume, jukebox.MinSlider, jukebox.MaxSlider, jukebox.MinVolume, jukebox.MaxVolume);
        }

        SendMessage(new JukeboxSetVolumeMessage(sentVolume));
    }

    public void UpdateDiskInfo()
    {
        if (_menu == null || !EntMan.TryGetComponent(Owner, out JukeboxComponent? jukebox))
            return;

        var diskEntity = EntMan.System<SharedJukeboxSystem>().GetInsertedDisk((Owner, jukebox));

        if (diskEntity.HasValue && EntMan.TryGetComponent(diskEntity.Value, out MetaDataComponent? metaData))
        {
            _menu.SetDiskName(metaData.EntityName);
            _menu.SetEjectButtonEnabled(true);
        }
        else
        {
            _menu.SetDiskName(null);
            _menu.SetEjectButtonEnabled(false);
        }

        // Update available songs based on the inserted disk
        PopulateMusic();
    }
    /// ADT-Tweak end
}
