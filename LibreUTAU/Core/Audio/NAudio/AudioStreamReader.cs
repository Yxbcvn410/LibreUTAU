using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace LibreUtau.Core.Audio.Build.NAudio {
    class AudioStreamReader : WaveStream, ISampleProvider {
        private readonly int destBytesPerSample;
        private readonly object lockObject;
        private readonly SampleChannel sampleChannel;
        private readonly int sourceBytesPerSample;
        private WaveStream readerStream;

        public AudioStreamReader(Stream WavStream) {
            lockObject = new object();
            CreateReaderStream(WavStream);
            sourceBytesPerSample = (readerStream.WaveFormat.BitsPerSample / 8) * readerStream.WaveFormat.Channels;
            sampleChannel = new SampleChannel(readerStream, false);
            destBytesPerSample = 4 * sampleChannel.WaveFormat.Channels;
            Length = SourceToDest(readerStream.Length);
        }

        public override long Length { get; }

        public override long Position {
            get { return SourceToDest(readerStream.Position); }
            set {
                lock (lockObject) { readerStream.Position = DestToSource(value); }
            }
        }

        public override WaveFormat WaveFormat {
            get { return sampleChannel.WaveFormat; }
        }

        /// <summary>
        ///     Reads audio from this sample provider
        /// </summary>
        /// <param name="buffer">Sample buffer</param>
        /// <param name="offset">Offset into sample buffer</param>
        /// <param name="count">Number of samples required</param>
        /// <returns>Number of samples read</returns>
        public int Read(float[] buffer, int offset, int count) {
            lock (lockObject) {
                return sampleChannel.Read(buffer, offset, count);
            }
        }

        private void CreateReaderStream(Stream WavStream) {
            readerStream = new WaveFileReader(WavStream);
            if (readerStream.WaveFormat.Encoding != WaveFormatEncoding.Pcm &&
                readerStream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat) {
                readerStream = WaveFormatConversionStream.CreatePcmStream(readerStream);
                readerStream = new BlockAlignReductionStream(readerStream);
            }
        }


        /// <summary>
        ///     Helper to convert source to dest bytes
        /// </summary>
        private long SourceToDest(long sourceBytes) {
            return destBytesPerSample * (sourceBytes / sourceBytesPerSample);
        }

        private long DestToSource(long destBytes) {
            return sourceBytesPerSample * (destBytes / destBytesPerSample);
        }

        /// <summary>
        ///     Reads from this wave stream
        /// </summary>
        /// <param name="buffer">Audio buffer</param>
        /// <param name="offset">Offset into buffer</param>
        /// <param name="count">Number of bytes required</param>
        /// <returns>Number of bytes read</returns>
        public override int Read(byte[] buffer, int offset, int count) {
            var waveBuffer = new WaveBuffer(buffer);
            int samplesRequired = count / 4;
            int samplesRead = Read(waveBuffer.FloatBuffer, offset / 4, samplesRequired);
            return samplesRead * 4;
        }
    }
}
