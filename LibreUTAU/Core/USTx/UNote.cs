using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibreUtau.Core.USTx {
    /// <summary>
    /// Music note.
    /// </summary>
    public class UNote : IComparable {
        public int PosTick;
        public int DurTick;
        public int NoteNum;
        public string Lyric = "a";
        public UPhoneme Phoneme { get; private set; }
        public Dictionary<string, UExpression> Expressions = new Dictionary<string, UExpression>();
        public PitchBendExpression PitchBend;
        public VibratoExpression Vibrato;
        public bool Error = false;
        public bool Selected = false;

        public int EndTick { get { return PosTick + DurTick; } }

        private UNote() {
            PitchBend = new PitchBendExpression(this);
            Vibrato = new VibratoExpression(this);
            Phoneme = new UPhoneme {Parent = this};
        }

        public static UNote Create() { return new UNote(); }

        public UNote Clone() {
            UNote _note = new UNote() {
                PosTick = PosTick,
                DurTick = DurTick,
                NoteNum = NoteNum,
                Lyric = Lyric
            };
            _note.Phoneme = Phoneme.Clone(_note);
            foreach (var pair in this.Expressions) _note.Expressions.Add(pair.Key, pair.Value.Clone(_note));
            _note.PitchBend = (PitchBendExpression)this.PitchBend.Clone(_note);
            return _note;
        }

        public string GetResamplerFlags() { return "Y0H0F0"; }

        public int CompareTo(object obj) {
            if (obj == null) return 1;

            if (!(obj is UNote other))
                throw new ArgumentException("CompareTo object is not a Note");

            if (other.PosTick < this.PosTick)
                return 1;
            else if (other.PosTick > this.PosTick)
                return -1;
            else if (other.GetHashCode() < this.GetHashCode())
                return 1;
            else if (other.GetHashCode() > this.GetHashCode())
                return -1;
            else
                return 0;
        }

        public override string ToString() {
            return
                $"\"{Lyric}\" Pos:{PosTick} Dur:{DurTick} Note:{NoteNum}{(Error ? " Error" : string.Empty)}{(Selected ? " Selected" : string.Empty)}";
        }
    }
}
