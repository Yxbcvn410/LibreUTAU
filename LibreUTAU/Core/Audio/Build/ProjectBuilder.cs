using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using LibreUtau.Core.Audio.Render.NAudio;
using LibreUtau.Core.Commands;
using LibreUtau.Core.ResamplerDriver;
using LibreUtau.Core.USTx;

namespace LibreUtau.Core.Audio.Build {
    public class ProjectBuilder : BackgroundWorker {
        private readonly UProject _project;
        private Action<int> ProgressReportCallback;

        public ProjectBuilder(UProject project) {
            _project = project;
            WorkerReportsProgress = true;
            ProgressReportCallback = progress => CommandDispatcher.Inst.ExecuteCmd(
                new ProgressBarNotification(progress, "Building audio..."), true);
        }

        public void SetProgressReportCallback(Action<int> callback) => ProgressReportCallback = callback;

        public void StartBuilding(bool force, Action<List<TrackSampleProvider>> FinishCallback) {
            if (this.IsBusy)
                return;
            this.DoWork += (s, e) => {
                e.Result = BuildAudio(_project, force);
            };
            this.RunWorkerCompleted += (s, e) => {
                if (e.Result == null)
                    return;
                CommandDispatcher.Inst.ExecuteCmd(new ProgressBarNotification(0, string.Format(string.Empty)));
                FinishCallback(e.Result as List<TrackSampleProvider>);
            };
            this.ProgressChanged += (s, e) => {
                ProgressReportCallback(e.ProgressPercentage);
            };
            this.RunWorkerAsync();
        }


        private List<TrackSampleProvider> BuildAudio(UProject project, bool force) {
            var trackSources = new List<TrackSampleProvider>();
            foreach (UTrack track in project.Tracks) {
                trackSources.Add(new TrackSampleProvider {Volume = MusicMath.DecibelToVolume(track.Volume)});
            }

            double maxProgress =
                    project.Parts.Sum(part => part is UVoicePart voicePart ? voicePart.ProgressWeight : 0),
                currentProgress = 0;
            FileInfo resamplerFile =
                new FileInfo(PathManager.Inst.GetPreviewEnginePath());
            IResamplerDriver engine =
                ResamplerDriver.ResamplerDriver.LoadEngine(resamplerFile.FullName);

            foreach (UPart part in project.Parts) {
                if (part is UVoicePart voicePart) {
                    var progress = currentProgress;

                    void ReportProgress(double p) {
                        this.ReportProgress((int)(100 * (progress + p * voicePart.ProgressWeight) / maxProgress));
                    }

                    voicePart.Build(ReportProgress, new BuildContext {Driver = engine, Project = project}, force);
                    currentProgress += voicePart.ProgressWeight;
                }

                trackSources[part.TrackNo].AddSource(part.RenderedTrack);
            }

            return trackSources;
        }
    }
}
