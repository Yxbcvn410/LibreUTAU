using System;
using System.IO;
using NAudio.Wave;

namespace LibreUtau.Core.Audio.NAudio {
    public class SampleToWaveStream : WaveStream {
        private readonly WaveChannel32 Reader;
        private readonly string TempFilename;

        public SampleToWaveStream(ISampleProvider sample) {
            TempFilename = Path.GetTempFileName();
            WaveFileWriter.CreateWaveFile(TempFilename, sample.ToWaveProvider());
            Reader = new WaveChannel32(new WaveFileReader(TempFilename)) {PadWithZeroes = false};
        }

        public override WaveFormat WaveFormat { get => Reader.WaveFormat; }
        public override long Length { get => Reader.Length; }
        public float Pan { get => Reader.Pan; set => Reader.Pan = value; }
        public float Volume { get => Reader.Volume; set => Reader.Volume = value; }

        public long BytesPerMs {
            get => Reader.WaveFormat.SampleRate * Reader.WaveFormat.BitsPerSample * Reader.WaveFormat.Channels / 8000;
        }

        public override long Position {
            get => Reader.Position;
            set {
                Reader.Position = Math.Min(value, Reader.Length);
            }
        }

        protected override void Dispose(bool disposing) {
            Reader.Dispose();
            File.Delete(TempFilename);
            base.Dispose(disposing);
        }

        public override int Read(byte[] buffer, int offset, int count) => Reader.Read(buffer, offset, count);
    }
}
