using System.ComponentModel;
using System.Windows.Media;
using LibreUtau.Core.Commands;

namespace LibreUtau.UI.Models {
    class ProgressBarViewModel : INotifyPropertyChanged, ICmdSubscriber {
        readonly object lockObject = new object();
        Brush _foreground;

        public Brush Foreground {
            set {
                _foreground = value;
                OnPropertyChanged("Foreground");
            }
            get { return _foreground; }
        }

        public int Progress { set; get; }
        public string Info { set; get; }

        public void SubscribeTo(ICmdPublisher publisher) {
            if (publisher != null) publisher.Subscribe(this);
        }

        public void OnCommandExecuted(UCommand cmd, bool isUndo) {
            if (cmd is ProgressBarNotification) Update((ProgressBarNotification)cmd);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public void Update(ProgressBarNotification cmd) {
            lock (lockObject) {
                Info = cmd.Info;
                Progress = cmd.Progress;
            }

            OnPropertyChanged("Progress");
            OnPropertyChanged("Info");
        }
    }
}
