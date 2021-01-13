using System.IO;
using LibreUtau.Core.ResamplerDriver;

namespace LibreUtau.Core.Audio.Build {
    public static class NoteCacheProvider {
        private static string UCacheDir = "";
        private static int maxCachedNotes = 10000;

        public static void SetCacheDir(string cacheDir) {
            if (Directory.Exists(cacheDir))
                UCacheDir = cacheDir;
        }

        public static void setCacheLimit(int limit) => maxCachedNotes = limit;

        private static string UUID(DriverModels.EngineInput input) {
            double hash = input.Velocity;
            foreach (double value in new[] {
                input.Offset, input.RequiredLength, input.Consonant, input.Cutoff, input.Volume, input.Modulation,
                input.Tempo, input.nPitchBend
            })
                hash = hash * 31 + value * 23;
            long integerHash = (long)hash;
            foreach (int value in input.pitchBend) integerHash = integerHash * 31 + value * 23;
            return
                $"{input.inputWaveFile.Replace(Path.DirectorySeparatorChar, '_')}_{input.NoteString}_{input.StrFlags}_{integerHash}";
        }

        public static Stream IntelligentResample(DriverModels.EngineInput input, IResamplerDriver driver,
            bool force = false) {
            if (!Directory.Exists(UCacheDir))
                return driver.DoResampler(input);

            var cacheFilename =
                driver.GetInfo().Name + UUID(input);
            var cachedNotePath = Path.Combine(UCacheDir, cacheFilename);

            return File.Exists(cachedNotePath) && !force
                ? new MemoryStream(File.ReadAllBytes(cachedNotePath))
                : ForceResample(input, driver);
        }

        public static Stream ForceResample(DriverModels.EngineInput input, IResamplerDriver driver) {
            var cacheFilename =
                driver.GetInfo().Name + UUID(input);
            var cachedNotePath = Path.Combine(UCacheDir, cacheFilename);
            // Render note and save it to cache
            var stream = driver.DoResampler(input);
            var sampleData = new byte[stream.Length];
            int offset = 0;
            int readLength = 1024;
            while (offset < sampleData.Length) {
                stream.Read(sampleData, offset, stream.Length - stream.Position < readLength
                    ? (int)(stream.Length -
                            stream.Position)
                    : readLength);
                offset += readLength;
            }

            File.WriteAllBytes(cachedNotePath, sampleData);

            return new MemoryStream(File.ReadAllBytes(cachedNotePath));
        }

        public static void CleanupCache(bool clearAll = false) {
            if (!Directory.Exists(UCacheDir))
                return;

            int counter = 0;
            foreach (var file in Directory.EnumerateFiles(UCacheDir)) {
                counter++;
                if (counter > maxCachedNotes || clearAll)
                    File.Delete(file);
            }
        }
    }
}
