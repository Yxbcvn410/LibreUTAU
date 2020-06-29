using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibreUtau.Core.USTx {
    /// <summary>
    /// The basic unit of synthesis.
    /// </summary>
    public class UPhoneme {
        public UNote Parent;
        public int DurTick;
        public string PhonemeString = "a";
        public string PhonemeRemapped { get { return AutoRemapped ? PhonemeString + RemappedBank : PhonemeString; } }
        public string RemappedBank = string.Empty;
        public bool AutoEnvelope = true;
        public bool AutoRemapped = true;

        public double Preutter;
        public double Overlap;
        public double TailIntrude;
        public double TailOverlap;
        public UOto Oto;
        public bool Overlapped = false;
        public bool OverlapCorrection = true;
        public EnvelopeExpression Envelope;

        public bool PhonemeError = false;

        public UPhoneme() { Envelope = new EnvelopeExpression(this.Parent) {ParentPhoneme = this}; }

        public UPhoneme Clone(UNote newParent) {
            var p = new UPhoneme() {Parent = newParent};
            return p;
        }
    }
}
