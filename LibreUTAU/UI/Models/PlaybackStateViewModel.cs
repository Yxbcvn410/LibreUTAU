using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using LibreUtau.Core.Audio;
using NAudio.Wave;

namespace LibreUtau.UI.Models {
    public class PlaybackStateViewModel : INotifyPropertyChanged {
        public PlaybackStateViewModel() {
            PlaybackManager.Inst.PlaybackStateChanged += (sender, e) => OnPropertyChanged(nameof(PlayPauseButtonStyle));
        }

        public Style PlayPauseButtonStyle {
            get => (PlaybackManager.Inst.PlaybackState == PlaybackState.Playing
                ? Application.Current.Resources["PauseButtonStyle"]
                : Application.Current.Resources["PlayButtonStyle"]) as Style;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
