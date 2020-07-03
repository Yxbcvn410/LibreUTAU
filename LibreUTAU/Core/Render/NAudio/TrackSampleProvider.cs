using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace LibreUtau.Core.Render {
    public class TrackSampleProvider : ISampleProvider {
        private PanningSampleProvider pan;
        private VolumeSampleProvider volume;
        private MixingSampleProvider mix;

        /// <summary>
        /// Pan. -1f (left) to 1f (right).
        /// </summary>
        public float Pan { set { pan.Pan = value; } get { return pan.Pan; } }

        /// <summary>
        /// Volume. 0f to 1f.
        /// </summary>
        public float Volume { set { volume.Volume = value; } get { return volume.Volume; } }

        public TrackSampleProvider() {
            mix = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            //pan = new PanningSampleProvider(mix);
            volume = new VolumeSampleProvider(mix);
        }

        public int Read(float[] buffer, int offset, int count) {
            return volume.Read(buffer, offset, count);
        }

        public WaveFormat WaveFormat {
            get { return volume.WaveFormat; }
        }

        public int Count { get => mix.MixerInputs.Count(); }

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
