using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibreUtau.Core.Render;
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
        public string Name = "New Part";
        public string Comment = string.Empty;

        #region Incremental build

        public abstract ISampleProvider RenderedTrack { get; }

        public abstract bool IsBuilt { get; }

        public virtual double ProgressWeight { get { return 1; } }

        public abstract void Build(Action<double> ProgressChangedCallback, BuildContext context);

        #endregion

        public int TrackNo;
        public int PosTick = 0;
        public virtual int DurTick { set; get; }
        public int EndTick { get { return PosTick + DurTick; } }

        public abstract int GetMinDurTick(UProject project);
    }

    public class UVoicePart : UPart {
        public SortedSet<UNote> Notes = new SortedSet<UNote>();

        private Dictionary<RenderItem, Stream> _builtState = new Dictionary<RenderItem, Stream>();

        private bool _needsRebuild = true;

        public void RequireRebuild() => _needsRebuild = true;

        public override ISampleProvider RenderedTrack {
            get {
                foreach (var renderPair in _builtState) {
                    renderPair.Value.Position = 0;
                    renderPair.Key.Sound = MemorySampleProvider.FromStream(renderPair.Value);
                }

                return new SequencingSampleProvider(from renderPair in _builtState
                    select new RenderItemSampleProvider(renderPair.Key));
            }
        }

        public override double ProgressWeight { get { return Notes.Count; } }

        public override bool IsBuilt { get { return !_needsRebuild; } }

        public override void Build(Action<double> ProgressChangedCallback, BuildContext context) {
            _needsRebuild = false;
            _builtState.Clear();
            var watch = new Stopwatch();
            watch.Start();
            Log.Information("Resampling start.");
            lock (this) {
                var cacheDir = PathManager.Inst.GetCachePath(context.Project.FilePath);
                var cacheFiles = Directory.EnumerateFiles(cacheDir).ToArray();
                int count = Notes.Count, phonemeProgress = 0;

                foreach (var note in Notes) {
                    if (string.IsNullOrEmpty(note.Phoneme.Oto.File)) {
                        Log.Warning($"Cannot find phoneme in note {note.Lyric}");
                        continue;
                    }

                    var item = new RenderItem(note.Phoneme, this, context.Project);
                    var engineArgs = DriverModels.CreateInputModel(item, 0);
                    var output = context.Driver.DoResampler(engineArgs);
                    item.Sound = MemorySampleProvider.FromStream(output);
                    _builtState.Add(item, output);

                    phonemeProgress++;
                    ProgressChangedCallback(phonemeProgress / (double)count);
                }
            }

            watch.Stop();
            Log.Information($"Resampling end, total time {watch.Elapsed}");
        }

        public override int GetMinDurTick(UProject project) {
            return Notes.Max(note => note.PosTick + note.DurTick);
        }
    }

    public class UWavePart : UPart {
        string _filePath;

        public string FilePath {
            set {
                _filePath = value;
                Name = Path.GetFileName(value);
            }
            get { return _filePath; }
        }

        public float[] Peaks;

        public int Channels;
        public int FileDurTick;
        public int HeadTrimTick = 0;
        public int TailTrimTick;

        public override ISampleProvider RenderedTrack {
            get {
                return new WaveToSampleProvider(new AudioFileReader(_filePath));
            }
        }

        public override bool IsBuilt { get => true; }
        public override void Build(Action<double> ProgressChangedCallback, BuildContext context) { }

        public override int DurTick {
            get { return FileDurTick - HeadTrimTick - TailTrimTick; }
            set { TailTrimTick = FileDurTick - HeadTrimTick - value; }
        }

        public override int GetMinDurTick(UProject project) { return 60; }
    }
}
