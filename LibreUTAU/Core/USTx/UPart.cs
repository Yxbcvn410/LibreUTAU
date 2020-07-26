using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LibreUtau.Core.USTx {
    public abstract class UPart {
        public string Comment = string.Empty;
        public string Name = "New Part";
        public int PosTick = 0;

        public int TrackNo;
        public virtual int DurTick { set; get; }
        public int EndTick { get { return PosTick + DurTick; } }

        public abstract int GetMinDurTick();
    }

    public class UVoicePart : UPart {
        public SortedSet<UNote> Notes = new SortedSet<UNote>();

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

        public override int DurTick {
            get { return FileDurTick - HeadTrimTick - TailTrimTick; }
            set { TailTrimTick = FileDurTick - HeadTrimTick - value; }
        }

        public override int GetMinDurTick() { return 60; }
    }
}
