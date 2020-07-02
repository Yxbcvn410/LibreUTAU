using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using LibreUtau.Core.Render;
using LibreUtau.Core.ResamplerDriver;
using LibreUtau.Core.USTx;

namespace LibreUtau.Core {
    public class ProjectBuilder : BackgroundWorker {
        private UProject _project;

        public ProjectBuilder(UProject project) {
            _project = project;
            WorkerReportsProgress = true;
        }

        public void Start(Action<List<TrackSampleProvider>> FinishCallback) {
            if (this.IsBusy)
                return;
            this.DoWork += (s, e) => {
                e.Result = BuildAudio(_project);
            };
            this.RunWorkerCompleted += (s, e) => {
                if (e.Result == null)
                    return;
                DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, string.Format(string.Empty)));
                FinishCallback(e.Result as List<TrackSampleProvider>);
            };
            this.ProgressChanged += (s, e) => {
                DocManager.Inst.ExecuteCmd(
                    new ProgressBarNotification(e.ProgressPercentage, "Building audio..."), true);
            };
            this.RunWorkerAsync();
        }

        private float DecibelToVolume(double db) {
            return (db <= -24)
                ? 0
                : (float)((db < -16) ? MusicMath.DecibelToLinear(db * 2 + 16) : MusicMath.DecibelToLinear(db));
            //TODO Что за костыль?
        }

        private List<TrackSampleProvider> BuildAudio(UProject project, bool forceRebuild = false) {
            var trackSources = new List<TrackSampleProvider>();
            foreach (UTrack track in project.Tracks) {
                trackSources.Add(new TrackSampleProvider {Volume = DecibelToVolume(track.Volume)});
            }

            double maxProgress =
                    project.Parts.Sum(part => part is UVoicePart voicePart ? voicePart.ProgressWeight : 0),
                currentProgress = 0;
            FileInfo ResamplerFile =
                new FileInfo(PathManager.Inst.GetPreviewEnginePath());
            IResamplerDriver engine =
                ResamplerDriver.ResamplerDriver.LoadEngine(ResamplerFile.FullName);

            foreach (UPart part in project.Parts) {
                if (part is UVoicePart voicePart){
                    var progress = currentProgress;

                    void ReportProgress(double p) {
                        this.ReportProgress((int)(100 * (progress + p * voicePart.ProgressWeight) / maxProgress));
                    }

                    if (!voicePart.IsBuilt || forceRebuild) {
                        voicePart.Build(ReportProgress, new BuildContext {Driver = engine, Project = project});
                        currentProgress += voicePart.ProgressWeight;
                    }
                }

                trackSources[part.TrackNo].AddSource(part.RenderedTrack,
                    TimeSpan.FromMilliseconds(project.TickToMillisecond(part.PosTick)));
            }

            return trackSources;
        }
    }
}
