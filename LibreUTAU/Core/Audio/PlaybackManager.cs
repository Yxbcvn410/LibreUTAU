using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using LibreUtau.Core.Audio.NAudio;
using LibreUtau.Core.Commands;
using NAudio.Wave;

namespace LibreUtau.Core.Audio {
    class PlaybackManager : ICmdSubscriber, INotifyPropertyChanged {
        private List<WaveOut> Devices;
        private long PlaybackPositionTick;
        private List<SampleToWaveStream> Tracks;

        public long PlaybackPosTick {
            get => PlaybackPositionTick = Tracks.Count == 0
                ? 0
                : Tracks.Max(track =>
                    CommandDispatcher.Inst.Project.MillisecondToTick(track.Position * 1000.0 / track.BytesPerSecond));
            private set => PlaybackPositionTick = value;
        }

        public Style PlayPauseButtonStyle {
            get => (PlaybackState == PlaybackState.Playing
                ? Application.Current.Resources["PauseButtonStyle"]
                : Application.Current.Resources["PlayButtonStyle"]) as Style;
        }


        public PlaybackState PlaybackState {
            get {
                if (Devices.Select(device => device.PlaybackState).Contains(PlaybackState.Playing))
                    return PlaybackState.Playing;
                if (Devices.Select(device => device.PlaybackState).Contains(PlaybackState.Paused))
                    return PlaybackState.Paused;
                return PlaybackState.Stopped;
            }
        }

        # region ICmdSubscriber

        public void OnCommandExecuted(UCommand cmd, bool isUndo) {
            if (cmd is SeekPlayPosTickNotification seekNotification) {
                OnClickPause();
                PlaybackPosTick = seekNotification.playPosTick;
                CommandDispatcher.Inst.ExecuteCmd(new SetPlayPosTickNotification(PlaybackPosTick));
            } else if (cmd is VolumeChangeNotification volumeNotification) {
                if (Tracks != null && Tracks.Count > volumeNotification.TrackNo) {
                    Tracks[volumeNotification.TrackNo].Volume = MusicMath.DecibelToVolume(volumeNotification.Volume);
                }
            } else if (cmd is PanChangeNotification panNotification) {
                if (Tracks != null && Tracks.Count > panNotification.TrackNo) {
                    Tracks[panNotification.TrackNo].Pan = MusicMath.PanToFloat(panNotification.Pan);
                }
            }
        }

        # endregion

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnClickPlay() {
            if (PlaybackState == PlaybackState.Stopped)
                PlaybackPosTick = 0;

            foreach (var track in Tracks)
                track.Position = (long)(CommandDispatcher.Inst.Project.TickToMillisecond(PlaybackPositionTick) *
                    track.BytesPerSecond / 1000);

            foreach (var device in Devices) device.Play();
            OnPlaybackStatusChanged();
        }

        public void OnClickPause() {
            foreach (var device in Devices) {
                device.Pause();
            }

            OnPlaybackStatusChanged();
        }

        public void Load(IEnumerable<SampleToWaveStream> trackSources, int deviceNumber = -1) {
            Devices.ForEach(device => device.Dispose());
            Tracks.ForEach(track => track.Dispose());

            Tracks = trackSources.ToList();
            Devices = Tracks.Select(track => {
                var device = new WaveOut {DeviceNumber = deviceNumber, NumberOfBuffers = 4};
                device.Init(track);
                device.PlaybackStopped += (sender, args) => OnPlaybackStatusChanged();
                return device;
            }).ToList();
            OnPlaybackStatusChanged();
        }

        private void OnPlaybackStatusChanged() =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlayPauseButtonStyle)));

        #region Singleton

        private PlaybackManager() {
            PlaybackPosTick = 0;
            Devices = new List<WaveOut>();
            Tracks = new List<SampleToWaveStream>();
            CommandDispatcher.Inst.AddSubscriber(this);
        }

        private static PlaybackManager _s;

        public static PlaybackManager Inst {
            get {
                if (_s == null) { _s = new PlaybackManager(); }

                return _s;
            }
        }

        #endregion
    }
}
