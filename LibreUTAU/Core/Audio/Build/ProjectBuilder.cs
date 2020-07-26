using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using LibreUtau.Core.Audio.Build.NAudio;
using LibreUtau.Core.Audio.Render;
using LibreUtau.Core.ResamplerDriver;
using LibreUtau.Core.USTx;
using LibreUtau.Core.Util;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Serilog;

namespace LibreUtau.Core.Audio.Build {
    public class ProjectBuilder : ProgressNotifyingTask {
        private readonly UProject _project;

        public ProjectBuilder(UProject project) {
            _project = project;
        }

        protected override string TaskInfo { get => Application.Current.Resources["tasks.build"] as string; }

        public void StartBuilding(bool force, Action<List<TrackSampleProvider>> FinishCallback) {
            if (this.IsBusy)
                return;
            this.DoWork += (s, e) => {
                e.Result = BuildAudio(_project, force);
            };
            this.RunWorkerCompleted += (s, e) => {
                if (e.Result == null)
                    return;
                FinishCallback(e.Result as List<TrackSampleProvider>);
            };
            this.RunWorkerAsync();
        }


        private List<TrackSampleProvider> BuildAudio(UProject project, bool force) {
            var trackSources = project.Tracks.Select(track => new TrackSampleProvider
                {Volume = MusicMath.DecibelToVolume(track.Volume)}).ToList();

            double maxProgress =
                    project.Parts.Sum(part => part is UVoicePart voicePart ? voicePart.Notes.Count : 0),
                currentProgress = 0;
            FileInfo resamplerFile =
                new FileInfo(PathManager.Inst.GetPreviewEnginePath());
            IResamplerDriver engine =
                ResamplerDriver.ResamplerDriver.LoadEngine(resamplerFile.FullName);

            Log.Debug("Audio build start");

            foreach (UPart part in project.Parts) {
                switch (part) {
                    case UVoicePart voicePart:
                        var renderItems = new List<RenderItem>();
                        lock (this) {
                            var cacheDir = PathManager.Inst.GetCachePath(project);
                            NoteCacheProvider.SetCacheDir(cacheDir);

                            foreach (var note in voicePart.Notes) {
                                if (this.CancellationPending)
                                    return new List<TrackSampleProvider>();

                                if (note.Phoneme.PhonemeError) {
                                    Log.Warning($"Phoneme error in note {note}");
                                    continue;
                                }

                                if (string.IsNullOrEmpty(note.Phoneme.Oto.File)) {
                                    Log.Warning($"Invalid wave location in note {note}");
                                    continue;
                                }

                                var item = new RenderItem(note.Phoneme, voicePart, project);
                                var engineArgs = DriverModels.CreateInputModel(item, 0);
                                var output = NoteCacheProvider.IntelligentResample(engineArgs, engine, force);
                                item.Sound = MemorySampleProvider.FromStream(output);
                                renderItems.Add(item);

                                currentProgress++;
                                this.ReportProgress(
                                    (int)(100 * currentProgress /
                                          maxProgress));
                            }
                        }

                        var sequence =
                            new SequencingSampleProvider(renderItems.Select(renderItem =>
                                new RenderItemSampleProvider(renderItem)));
                        trackSources[part.TrackNo].AddSource(sequence);
                        break;
                    case UWavePart wavePart:
                        trackSources[part.TrackNo]
                            .AddSource(new WaveToSampleProvider(new AudioFileReader(wavePart.FilePath)));
                        break;
                }
            }

            Log.Debug("Audio build done");

            return trackSources;
        }
    }
}
