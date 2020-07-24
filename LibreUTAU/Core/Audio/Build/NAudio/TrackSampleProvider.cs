using System.Linq;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace LibreUtau.Core.Audio.Render.NAudio {
    public class TrackSampleProvider : ISampleProvider {
        private readonly MixingSampleProvider mix;
        private readonly VolumeSampleProvider volume;
        private PanningSampleProvider pan;

        public TrackSampleProvider() {
            mix = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            //pan = new PanningSampleProvider(mix);
            volume = new VolumeSampleProvider(mix);
        }

        /// <summary>
        ///     Pan. -1f (left) to 1f (right).
        /// </summary>
        public float Pan { set { pan.Pan = value; } get { return pan.Pan; } }

        /// <summary>
        ///     Volume. 0f to 1f.
        /// </summary>
        public float Volume { set { volume.Volume = value; } get { return volume.Volume; } }

        public int Count { get => mix.MixerInputs.Count(); }

        public int Read(float[] buffer, int offset, int count) {
            return volume.Read(buffer, offset, count);
        }

        public WaveFormat WaveFormat {
            get { return volume.WaveFormat; }
        }

        public void AddSource(ISampleProvider source) {
            if (source?.WaveFormat == null)
                return;

            switch (source.WaveFormat.Channels) {
                case 1:
                    mix.AddMixerInput(new MonoToStereoSampleProvider(source));
                    break;
                case 2:
                    mix.AddMixerInput(source);
                    break;
                default:
                    return;
            }
        }
    }
}
