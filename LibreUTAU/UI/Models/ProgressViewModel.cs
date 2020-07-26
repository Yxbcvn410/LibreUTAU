using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using LibreUtau.Core.Commands;

namespace LibreUtau.UI.Models {
    class ProgressViewModel : INotifyPropertyChanged, ICmdSubscriber {
        readonly object lockObject = new object();
        Brush _foreground;

        private TimeSpan ETA = TimeSpan.Zero;

        private DateTime StartTime = DateTime.Now;

        private bool Visible;

        public Brush Foreground {
            set {
                _foreground = value;
                OnPropertyChanged("Foreground");
            }
            get { return _foreground; }
        }

        public Visibility Visibility { get => Visible ? Visibility.Visible : Visibility.Hidden; }

        public int Progress { set; get; }
        public string Info { set; get; }

        public string EtaString {
            get =>
                $"ETA: {(DateTime.Now - StartTime > TimeSpan.FromSeconds(1) ? ETA.ToString(@"hh\:mm\:ss") : "??:??:??")}"
            ;
        }

        public bool MenuEnabled {
            get => !Visible;
        }

        public void SubscribeTo(ICmdPublisher publisher) {
            publisher?.Subscribe(this);
        }

        public void OnCommandExecuted(UCommand cmd, bool isUndo) {
            switch (cmd) {
                case ProgressIndicatorUpdateNotification notification:
                    lock (lockObject) {
                        Info = notification.Info;
                        Progress = notification.Progress;
                        if (DateTime.Now - StartTime > TimeSpan.FromSeconds(1))
                            ETA = TimeSpan.FromSeconds(
                                (DateTime.Now - StartTime).TotalSeconds * (100 - Progress) / Progress);
                    }

                    OnPropertyChanged("Progress");
                    OnPropertyChanged("Info");
                    OnPropertyChanged("EtaString");
                    break;
                case ProgressIndicatorVisibilityNotification notification:
                    lock (lockObject) {
                        Visible = notification.Visible;
                        if (Visible)
                            StartTime = DateTime.Now;
                    }

                    OnPropertyChanged("Visibility");
                    OnPropertyChanged("MenuEnabled");
                    break;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static void Cancel() {
            CommandDispatcher.Inst.ExecuteCmd(new ProgressIndicatorCancelNotification());
        }

        protected void OnPropertyChanged(string name) {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
