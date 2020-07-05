using System;
using System.Collections.Generic;
using NAudio.Utils;
using NAudio.Wave;

namespace LibreUtau.Core.Audio.Render.NAudio {
    /// <summary>
    ///     A sample provider mixer, allowing RenderItemSampleProviders to be sequenced,
    ///     modified from MixingSampleProvider
    /// </summary>
    class SequencingSampleProvider : ISampleProvider {
        private const int maxInputs = 2048; // protect ourselves against doing something silly
        private readonly List<RenderItemSampleProvider> sources;
        private float[] sourceBuffer;

        /// <summary>
        ///     Creates a new SequencingSampleProvider, based on the given inputs
        /// </summary>
        public SequencingSampleProvider(IEnumerable<RenderItemSampleProvider> sources) {
            this.sources = new List<RenderItemSampleProvider>();
            foreach (var source in sources) {
                AddSequencingInput(source);
            }
        }

        /// <summary>
        ///     The output WaveFormat of this sample provider
        /// </summary>
        public WaveFormat WaveFormat { get; private set; }

        /// <summary>
        ///     Reads samples from this sample provider
        /// </summary>
        /// <param name="buffer">Sample buffer</param>
        /// <param name="offset">Offset into sample buffer</param>
        /// <param name="count">Number of samples required</param>
        /// <returns>Number of samples read</returns>
        public int Read(float[] buffer, int offset, int count) {
            int outputSamples = 0;
            this.sourceBuffer = BufferHelpers.Ensure(this.sourceBuffer, count);
            lock (sources) {
                int index = sources.Count - 1;
                while (index >= 0) {
                    var source = sources[index];
                    int samplesRead = source.Read(this.sourceBuffer, 0, count);
                    int outIndex = offset;
                    for (int n = 0; n < samplesRead; n++) {
                        if (n >= outputSamples) {
                            buffer[outIndex++] = this.sourceBuffer[n];
                        } else {
                            buffer[outIndex++] += this.sourceBuffer[n];
                        }
                    }

                    outputSamples = Math.Max(samplesRead, outputSamples);
                    index--;
                }
            }

            return outputSamples;
        }

        /// <summary>
        ///     Adds a new mixer input
        /// </summary>
        /// <param name="mixerInput">Mixer input</param>
        public void AddSequencingInput(RenderItemSampleProvider mixerInput) {
            // we'll just call the lock around add since we are protecting against an AddMixerInput at
            // the same time as a Read, rather than two AddMixerInput calls at the same time
            lock (sources) {
                if (this.sources.Count >= maxInputs) {
                    throw new InvalidOperationException("Too many mixer inputs");
                }

                this.sources.Add(mixerInput);
            }

            if (this.WaveFormat == null) {
                this.WaveFormat = mixerInput.WaveFormat;
            } else {
                if (this.WaveFormat.SampleRate != mixerInput.WaveFormat.SampleRate ||
                    this.WaveFormat.Channels != mixerInput.WaveFormat.Channels) {
                    throw new ArgumentException("All mixer inputs must have the same WaveFormat");
                }
            }
        }

        /// <summary>
        ///     Removes a mixer input
        /// </summary>
        /// <param name="mixerInput">Mixer input to remove</param>
        public void RemoveMixerInput(RenderItemSampleProvider mixerInput) {
            lock (sources) {
                this.sources.Remove(mixerInput);
            }
        }

        /// <summary>
        ///     Removes all mixer inputs
        /// </summary>
        public void RemoveAllMixerInputs() {
            lock (sources) {
                this.sources.Clear();
            }
        }
    }
}
