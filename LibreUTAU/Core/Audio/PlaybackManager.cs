using System;
using System.Collections.Generic;
using System.Linq;
using LibreUtau.Core.Audio.NAudio;
using LibreUtau.Core.Commands;
using NAudio.Wave;

namespace LibreUtau.Core.Audio {
    class PlaybackManager : ICmdSubscriber {
        private List<WaveOut> Devices;
        private List<SampleToWaveStream> Tracks;

        # region ICmdSubscriber

        public void OnCommandExecuted(UCommand cmd, bool isUndo) {
            if (cmd is VolumeChangeNotification volumeNotification) {
                if (Tracks != null && Tracks.Count > volumeNotification.TrackNo) {
                    Tracks[volumeNotification.TrackNo].Volume = MusicMath.DecibelToVolume(volumeNotification.Volume);
                }
            } else if (cmd is PanChangeNotification panNotification) {
                if (Tracks != null && Tracks.Count > panNotification.TrackNo) {
                    Tracks[panNotification.TrackNo].Pan = MusicMath.PanToFloat(panNotification.Pan);
                }
            } else if (!(cmd is UNotification)) {
                Stop();
            }
        }

        # endregion

        public void Load(IEnumerable<SampleToWaveStream> trackSources, int deviceNumber = -1) {
            Devices.ForEach(device => device.Dispose());
            Tracks.ForEach(track => track.Dispose());

            Tracks = trackSources.ToList();
            Devices = Tracks.Select(track => {
                var device = new WaveOut {DeviceNumber = deviceNumber, NumberOfBuffers = 4};
                device.Init(track);
                device.PlaybackStopped += (sender, args) => {
                    bool allStopped = true;
                    foreach (var dev in Devices)
                        allStopped = allStopped && (dev.PlaybackState == PlaybackState.Stopped);
                    if (allStopped)
                        PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
                };
                return device;
            }).ToList();
        }

        public void Play() {
            if (PlaybackState == PlaybackState.Stopped)
                PlaybackPosTick = 0;

            foreach (var track in Tracks)
                track.Position = (long)(CommandDispatcher.Inst.Project.TickToMillisecond(PlaybackPosTick) *
                                        track.BytesPerMs);

            foreach (var device in Devices) device.Play();
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Pause() {
            foreach (var device in Devices) device.Pause();
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Stop() {
            foreach (var device in Devices) device.Stop();
            PlaybackPosTick = 0;
            PlaybackStateChanged?.Invoke(this, EventArgs.Empty);
        }

        #region Singleton

        private PlaybackManager() {
            Devices = new List<WaveOut>();
            Tracks = new List<SampleToWaveStream>();
            CommandDispatcher.Inst.AddSubscriber(this);
        }

        private static PlaybackManager _s;

        public static PlaybackManager Inst {
            get { return _s ?? (_s = new PlaybackManager()); }
        }

        #endregion

        #region Properties

        public long PlaybackPosTick {
            get {
                if (Tracks.Count == 0)
                    return 0;
                var maxPosition = Tracks.Max(track => track.Position);
                return CommandDispatcher.Inst.Project.MillisecondToTick(maxPosition / (double)Tracks[0].BytesPerMs);
            }
            set {
                foreach (var track in Tracks)
                    track.Position = (long)(CommandDispatcher.Inst.Project.TickToMillisecond(value) *
                                            track.BytesPerMs);
            }
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

        public event EventHandler PlaybackStateChanged;

        #endregion
    }
}
