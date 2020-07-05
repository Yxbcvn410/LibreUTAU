using System.Collections.Generic;
using LibreUtau.Core.Audio.Render.NAudio;
using NAudio.Wave;

namespace LibreUtau.Core.Audio.Render {
    class RenderDispatcher {
        public List<RenderItem> RenderItems = new List<RenderItem>();

        public void WriteToFile(string file) {
            WaveFileWriter.CreateWaveFile16(file, GetMixingSampleProvider());
        }

        public SequencingSampleProvider GetMixingSampleProvider() {
            List<RenderItemSampleProvider> segmentProviders = new List<RenderItemSampleProvider>();
            foreach (var item in RenderItems) segmentProviders.Add(new RenderItemSampleProvider(item));
            return new SequencingSampleProvider(segmentProviders);
        }
    }
}
