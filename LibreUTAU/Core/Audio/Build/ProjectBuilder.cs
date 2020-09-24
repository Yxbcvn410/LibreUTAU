using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using LibreUtau.Core.Audio.Build.NAudio;
using LibreUtau.Core.Audio.NAudio;
using LibreUtau.Core.Audio.Render;
using LibreUtau.Core.ResamplerDriver;
using LibreUtau.Core.USTx;
using LibreUtau.UI.Models;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Serilog;

namespace LibreUtau.Core.Audio.Build {
    public class ProjectBuilder : BackgroundWorker {
        private readonly UProject _project;

        public ProjectBuilder(UProject project) {
            _project = project;
        }

        public void StartBuilding(bool force, Action<List<SampleToWaveStream>> finishCallback) {
            if (this.IsBusy)
                return;
            ProgressModel.Inst.AssignTask(this);
            ProgressModel.Inst.Info = Application.Current.Resources["tasks.build"] as string;
            this.DoWork += (s, e) => {
                e.Result = BuildAudio(_project, force);
            };
            this.RunWorkerCompleted += (s, e) => {
                if (e.Error != null)
                    return;
                if (e.Result == null)
                    return;
                finishCallback(e.Result as List<SampleToWaveStream>);
            };
            this.RunWorkerAsync();
        }


        private List<SampleToWaveStream> BuildAudio(UProject project, bool force) {
            var trackSources = project.Tracks.Select(
                track => new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
            ).ToList();

            double maxProgress =
                    project.Parts.Sum(part => part is UVoicePart voicePart ? voicePart.Notes.Count : 0),
                currentProgress = 0;
            if (!File.Exists(PathManager.Inst.GetPreviewEnginePath()))
                throw new BackgroundTaskException("tasks.build.noresampler");
            FileInfo resamplerFile =
                new FileInfo(PathManager.Inst.GetPreviewEnginePath());
            IResamplerDriver engine =
                ResamplerDriver.ResamplerDriver.LoadEngine(resamplerFile.FullName);

            Log.Debug("Audio build started");

            foreach (UPart part in project.Parts) {
                switch (part) {
                    case UVoicePart voicePart:
                        var renderItems = new List<RenderItem>();
                        lock (this) {
                            var cacheDir = PathManager.Inst.GetCachePath(project);
                            NoteCacheProvider.SetCacheDir(cacheDir);

                            foreach (var note in voicePart.Notes) {
                                if (this.CancellationPending)
                                    return new List<SampleToWaveStream>();

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

                        ISampleProvider sequence =
                            new SequencingSampleProvider(renderItems.Select(renderItem =>
                                new RenderItemSampleProvider(renderItem)));
                        if (sequence.WaveFormat.Channels == 1)
                            sequence = new MonoToStereoSampleProvider(sequence);
                        trackSources[part.TrackNo].AddMixerInput(sequence);

                        break;
                    case UWavePart wavePart:
                        ISampleProvider wave = new AudioFileReader(wavePart.FilePath).ToSampleProvider();
                        if (wave.WaveFormat.Channels == 1)
                            wave = new MonoToStereoSampleProvider(wave);
                        trackSources[part.TrackNo]
                            .AddMixerInput(wave);
                        break;
                }
            }

            Log.Debug("Audio build done");
            project.Built = true;
            var tracks = trackSources.Select(input => new SampleToWaveStream(input)).ToList();
            for (int i = 0; i < tracks.Count; i++) {
                tracks[i].Volume = MusicMath.DecibelToVolume(project.Tracks[i].Volume);
                tracks[i].Pan = MusicMath.PanToFloat(project.Tracks[i].Pan);
            }

            return tracks;
        }
    }
}
