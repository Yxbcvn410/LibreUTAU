using System;
using System.ComponentModel;
using System.Timers;
using System.Windows;
using System.Windows.Media;

namespace LibreUtau.UI.Models {
    class BackgroundTaskException : Exception {
        public readonly string Caption;
        public readonly string Text;

        public BackgroundTaskException(string resourceKey) {
            Text = Application.Current.Resources[$"{resourceKey}.message"] is string message
                ? message
                : Application.Current.Resources["tasks.error.message"] as string;

            Caption = Application.Current.Resources[$"{resourceKey}.caption"] is string caption
                ? caption
                : Application.Current.Resources["tasks.error.title"] as string;
        }
    }

    class ProgressModel : INotifyPropertyChanged {
        private readonly Timer EtaUpdater = new Timer(1000);
        readonly object lockObject = new object();

        private bool _active;

        private Brush _foreground;

        private string _info;
        private int _progress;

        private BackgroundWorker CurrentWorker;

        private TimeSpan ETA = TimeSpan.Zero;

        private DateTime StartTime = DateTime.Now;

        public Brush Foreground {
            set {
                _foreground = value;
                OnPropertyChanged("Foreground");
            }
            get { return _foreground; }
        }

        public bool Active {
            get => _active;
            set {
                _active = value;
                EtaUpdater.Enabled = _active;
                if (_active)
                    StartTime = DateTime.Now;
                OnPropertyChanged("Visibility");
                OnPropertyChanged("MenuEnabled");
                OnPropertyChanged("EtaString");
            }
        }

        public Visibility Visibility { get => Active ? Visibility.Visible : Visibility.Hidden; }

        public int Progress {
            set {
                _progress = value;
                OnPropertyChanged("Progress");
            }
            get => _progress;
        }

        public string Info {
            set {
                _info = value;
                OnPropertyChanged("Info");
            }
            get => _info;
        }

        public string EtaString {
            get =>
                $"ETA: {(DateTime.Now - StartTime > TimeSpan.FromSeconds(1) ? ETA.ToString(@"hh\:mm\:ss") : "??:??:??")}"
            ;
        }

        public bool MenuEnabled {
            get => !Active;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void AssignTask(BackgroundWorker worker) {
            CurrentWorker = worker;
            CurrentWorker.WorkerReportsProgress = CurrentWorker.WorkerSupportsCancellation = true;
            CurrentWorker.DoWork += (sender, args) => Active = true;
            CurrentWorker.ProgressChanged += (sender, args) => {
                lock (lockObject) {
                    Progress = args.ProgressPercentage;
                    if (DateTime.Now - StartTime > TimeSpan.FromSeconds(1))
                        ETA = TimeSpan.FromSeconds(
                            (DateTime.Now - StartTime).TotalSeconds * (100 - Progress) / Progress);
                }
            };
            CurrentWorker.RunWorkerCompleted += (sender, args) => {
                Active = false;
                if (args.Error is BackgroundTaskException exception) {
                    MessageBox.Show(exception.Text, exception.Caption, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
        }

        public void Cancel() {
            CurrentWorker.CancelAsync();
        }

        private void OnPropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #region Singleton

        private ProgressModel() {
            EtaUpdater.Elapsed += (sender, args) => OnPropertyChanged("EtaString");
        }

        private static ProgressModel _s;

        public static ProgressModel Inst { get => _s ?? (_s = new ProgressModel()); }

        #endregion
    }
}
