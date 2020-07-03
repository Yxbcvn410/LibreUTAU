﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using LibreUtau.Core.Formats;

namespace LibreUtau.UI {
    public partial class SplashScreen {
        public SplashScreen() {
            InitializeComponent();

            var info = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            var currentVersion = new Version(info.ProductVersion);
            TitleText.Text = "LibreUtau v" + currentVersion;

            ProgressText.Text = "Loading...";

            BackgroundWorker loadAppWorker = new BackgroundWorker();
            loadAppWorker.WorkerReportsProgress = true;
            loadAppWorker.DoWork += (sender, args) => LoadApp(sender as BackgroundWorker);
            loadAppWorker.ProgressChanged += (sender, args) => {
                var dispatcher = this.Dispatcher;
                dispatcher?.Invoke(delegate {
                    if (args.UserState != null) ProgressText.Text = args.UserState as string;
                });
            };
            loadAppWorker.RunWorkerCompleted += (sender, args) => OpenMainWindow();
            loadAppWorker.RunWorkerAsync();
        }

        private void LoadApp(BackgroundWorker worker) {
            worker.ReportProgress(0, "Loading singers...");
            UtauSoundbank.FindAllSingers();

            var partManager = new Core.PartManager();
            worker.ReportProgress(0, "Loading UI...");
        }

        private void OpenMainWindow() {
            var t = new Thread((o => {
                MainWindow mw = new MainWindow();
                mw.Show();
                var dispatcher = this.Dispatcher;
                dispatcher?.Invoke(this.Hide);
                System.Windows.Threading.Dispatcher.Run();
            }));
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
        }
    }
}
