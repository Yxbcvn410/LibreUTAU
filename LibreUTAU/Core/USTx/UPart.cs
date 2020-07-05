using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LibreUtau.Core.Audio.Build;
using LibreUtau.Core.Audio.Render;
using LibreUtau.Core.Audio.Render.NAudio;
using LibreUtau.Core.ResamplerDriver;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Serilog;

namespace LibreUtau.Core.USTx {
    public struct BuildContext {
        public UProject Project;
        internal IResamplerDriver Driver;
    }

    public abstract class UPart {
        public string Comment = string.Empty;
        public string Name = "New Part";
        public int PosTick = 0;

        public int TrackNo;

        public abstract ISampleProvider RenderedTrack { get; }
        public virtual int DurTick { set; get; }
        public int EndTick { get { return PosTick + DurTick; } }

        public abstract int GetMinDurTick();
    }

    public class UVoicePart : UPart {
        public SortedSet<UNote> Notes = new SortedSet<UNote>();

        private SequencingSampleProvider sequence;

        public override ISampleProvider RenderedTrack { get => sequence; }

        public double ProgressWeight { get { return Notes.Count; } }

        public void Build(Action<double> ProgressChangedCallback, BuildContext buildContext) {
            var context = buildContext is BuildContext context1 ? context1 : default;
            var watch = new Stopwatch();
            watch.Start();
            Log.Information("Resampling start.");
            var renderItems = new List<RenderItem>();
            lock (this) {
                var cacheDir = PathManager.Inst.GetCachePath(context.Project.FilePath);
                NoteCacheProvider.SetCacheDir(cacheDir);
                int count = Notes.Count, phonemeProgress = 0;

                foreach (var note in Notes) {
                    if (note.Phoneme.PhonemeError) {
                        Log.Warning($"Phoneme error in note {note}");
                        continue;
                    }

                    if (string.IsNullOrEmpty(note.Phoneme.Oto.File)) {
                        Log.Warning($"Invalid wave location in note {note}");
                        continue;
                    }

                    var item = new RenderItem(note.Phoneme, this, context.Project);
                    var engineArgs = DriverModels.CreateInputModel(item, 0);
                    var output = NoteCacheProvider.LazyResample(engineArgs, context.Driver);
                    item.Sound = MemorySampleProvider.FromStream(output);
                    renderItems.Add(item);

                    phonemeProgress++;
                    ProgressChangedCallback(phonemeProgress / (double)count);
                }
            }

            watch.Stop();
            Log.Information($"Resampling end, total time {watch.Elapsed}");
            sequence = new SequencingSampleProvider(from renderItem in renderItems
                select new RenderItemSampleProvider(renderItem));
        }

        public override int GetMinDurTick() {
            return Notes.Count > 0 ? Notes.Max(note => note.PosTick + note.DurTick) : 1;
        }
    }

    public class UWavePart : UPart {
        string _filePath;

        public int Channels;
        public int FileDurTick;
        public int HeadTrimTick = 0;

        public float[] Peaks;
        public int TailTrimTick;

        public string FilePath {
            set {
                _filePath = value;
                Name = Path.GetFileName(value);
            }
            get { return _filePath; }
        }

        public override ISampleProvider RenderedTrack {
            get {
                return new WaveToSampleProvider(new AudioFileReader(_filePath));
            }
        }

        public override int DurTick {
            get { return FileDurTick - HeadTrimTick - TailTrimTick; }
            set { TailTrimTick = FileDurTick - HeadTrimTick - value; }
        }

        public override int GetMinDurTick() { return 60; }
    }
}
