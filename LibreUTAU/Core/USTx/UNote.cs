using System;
using System.Collections.Generic;

namespace LibreUtau.Core.USTx {
    /// <summary>
    ///     Music note.
    /// </summary>
    public class UNote : IComparable {
        public int DurTick;
        public bool Error = false;
        public Dictionary<string, UExpression> Expressions = new Dictionary<string, UExpression>();
        public string Lyric = "a";
        public int NoteNum;
        public PitchBendExpression PitchBend;
        public int PosTick;
        public VibratoExpression Vibrato;

        private UNote() {
            PitchBend = new PitchBendExpression(this);
            Vibrato = new VibratoExpression(this);
            Phoneme = new UPhoneme {Parent = this};
        }

        public UPhoneme Phoneme { get; private set; }

        public int EndTick { get { return PosTick + DurTick; } }

        public int CompareTo(object obj) {
            if (obj == null) return 1;

            if (!(obj is UNote other))
                throw new ArgumentException("CompareTo object is not a Note");

            if (other.PosTick < this.PosTick)
                return 1;
            if (other.PosTick > this.PosTick)
                return -1;
            if (other.GetHashCode() < this.GetHashCode())
                return 1;
            if (other.GetHashCode() > this.GetHashCode())
                return -1;
            return 0;
        }

        public static UNote Create() { return new UNote(); }

        public UNote Clone() {
            UNote _note = new UNote {
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

        public void UpdatePhoneme(USinger singer) {
            Phoneme.PhonemeString = Lyric;

            if (Phoneme.AutoRemapped) {
                if (Phoneme.PhonemeString.StartsWith("?")) {
                    Phoneme.PhonemeString = Phoneme.PhonemeString.Substring(1);
                    Phoneme.AutoRemapped = false;
                } else {
                    string noteString = MusicMath.GetNoteString(NoteNum);
                    if (singer.PitchMap.ContainsKey(noteString))
                        Phoneme.RemappedBank = singer.PitchMap[noteString];
                }
            }

            if (singer.AliasMap.ContainsKey(Phoneme.PhonemeRemapped)) {
                Phoneme.Oto = singer.AliasMap[Phoneme.PhonemeRemapped];
                Phoneme.PhonemeError = false;
                Phoneme.Overlap = Phoneme.Oto.Overlap;
                Phoneme.PreUtter = Phoneme.Oto.Preutter;
                int vel = (int)Phoneme.Parent.Expressions["velocity"].Data;
                if (vel != 100) {
                    double stretchRatio = Math.Pow(2, 1.0 - (double)vel / 100);
                    Phoneme.Overlap *= stretchRatio;
                    Phoneme.PreUtter *= stretchRatio;
                }
            } else {
                Phoneme.PhonemeError = true;
                Phoneme.Overlap = 0;
                Phoneme.PreUtter = 0;
            }
        }

        public string GetResamplerFlags() { return "Y0H0F0"; }

        public override string ToString() {
            return
                $"\"{Lyric}\" Pos:{PosTick} Dur:{DurTick} Note:{NoteNum}{(Error ? " Error" : string.Empty)}";
        }
    }
}
