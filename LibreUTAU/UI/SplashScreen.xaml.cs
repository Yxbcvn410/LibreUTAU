using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LibreUtau.Core.Formats;
using LibreUtau.Core.Render;

namespace LibreUtau.UI {
    public partial class SplashScreen : Window {
        public SplashScreen() {
            InitializeComponent();

            var info = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            var current_version = new Version(info.ProductVersion);
            TitleText.Text = "LibreUtau v" + current_version;

            ProgressText.Text = "Loading...";

            BackgroundWorker loadAppWorker = new BackgroundWorker();
            loadAppWorker.WorkerReportsProgress = true;
            loadAppWorker.DoWork += (sender, args) => LoadApp(sender as BackgroundWorker);
            loadAppWorker.ProgressChanged += (sender, args) => this.Dispatcher.Invoke(delegate {
                ProgressText.Text = args.UserState as string;
            });
            loadAppWorker.RunWorkerCompleted += (sender, args) => OpenMainWindow();
            loadAppWorker.RunWorkerAsync();
        }

        private void LoadApp(BackgroundWorker worker) {
            worker.ReportProgress(0, "Loading singers...");
            UtauSoundbank.FindAllSingers();

            var pm = new Core.PartManager();
            worker.ReportProgress(0, "Loading UI...");
        }

        private void OpenMainWindow() {
            var t = new Thread((o => {
                MainWindow mw = new MainWindow();
                mw.Show();
                this.Dispatcher.Invoke(delegate { this.Close(); });
                System.Windows.Threading.Dispatcher.Run();
            }));
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }
    }
}
