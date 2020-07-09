using System;
using System.Collections.Generic;
using System.Linq;
using LibreUtau.Core.Audio.Render.NAudio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace LibreUtau.Core.Audio.Render {
    public enum ExportFormat {
        WAV = 0,
        MP3 = 1,
        WMA = 2,
        AAC = 3
    }

    public static class FilterDispatcher {
        private static readonly Dictionary<ExportFormat, string> FormatsMap = new Dictionary<ExportFormat, string> {
            {ExportFormat.WAV, "WAV|*.wav"},
            {ExportFormat.MP3, "MP3|*.mp3"},
            {ExportFormat.WMA, "WMA|*.wma"},
            {ExportFormat.AAC, "AAC|*.aac"}
        };

        public static string GetFilter() => String.Join("|",
            Enum.GetValues(typeof(ExportFormat)).Cast<ExportFormat>().ToList().Select(format => FormatsMap[format]));
    }

    static class RenderDispatcher {
        public static void ExportSound(string outputFile, List<TrackSampleProvider> tracks, ExportFormat format) {
            MixingSampleProvider master = new MixingSampleProvider(tracks);
            var masterFinal = master.FollowedBy(new SilenceProvider(master.WaveFormat).ToSampleProvider()
                .Take(TimeSpan.FromSeconds(0.5)));
            switch (format) {
                case ExportFormat.WAV:
                    WaveFileWriter.CreateWaveFile(outputFile, new SampleToWaveProvider(masterFinal));
                    break;
                case ExportFormat.MP3:
                    MediaFoundationEncoder.EncodeToMp3(new SampleToWaveProvider(masterFinal), outputFile);
                    break;
                case ExportFormat.AAC:
                    MediaFoundationEncoder.EncodeToAac(new SampleToWaveProvider(masterFinal), outputFile);
                    break;
                case ExportFormat.WMA:
                    MediaFoundationEncoder.EncodeToWma(new SampleToWaveProvider(masterFinal), outputFile);
                    break;
            }
        }
    }
}
