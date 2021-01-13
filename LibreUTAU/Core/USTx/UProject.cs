using System;
using System.Collections.Generic;
using LibreUtau.Core.Commands;

namespace LibreUtau.Core.USTx {
    public class UProject {
        public readonly Dictionary<string, UExpression> ExpressionTable = new Dictionary<string, UExpression>();
        public int BeatPerBar = 4;
        public int BeatUnit = 4;
        public double BPM = 120;
        public string CacheDir = "UCache";
        public string Comment = string.Empty;
        public string FilePath;

        public string Name = "New Project";
        public string OutputDir = "Vocal";
        public List<UPart> Parts = new List<UPart>();
        public int Resolution = 480;
        public bool Saved = false;

        public List<UTrack> Tracks = new List<UTrack>();

        public void RegisterExpression(UExpression exp) {
            if (!ExpressionTable.ContainsKey(exp.Name))
                ExpressionTable.Add(exp.Name, exp);
        }

        public UNote CreateNote() {
            UNote note = UNote.Create();
            foreach (var pair in ExpressionTable) { note.Expressions.Add(pair.Key, pair.Value.Clone(note)); }

            note.PitchBend.Points[0].X = -25;
            note.PitchBend.Points[1].X = 25;
            return note;
        }

        public UNote CreateNote(int noteNum, int posTick, int durTick) {
            var note = CreateNote();
            note.NoteNum = noteNum;
            note.PosTick = posTick;
            note.DurTick = durTick;
            note.PitchBend.Points[1].X =
                Math.Min(25, CommandDispatcher.Inst.Project.TickToMillisecond(note.DurTick) / 2);
            return note;
        }

        public int MillisecondToTick(double ms) {
            return MusicMath.MillisecondToTick(ms, BPM, BeatUnit, Resolution);
        }

        public double TickToMillisecond(double tick) {
            return MusicMath.TickToMillisecond(tick, BPM, BeatUnit, Resolution);
        }
    }
}
