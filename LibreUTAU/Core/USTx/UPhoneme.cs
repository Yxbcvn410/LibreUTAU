namespace LibreUtau.Core.USTx {
    /// <summary>
    ///     The basic unit of synthesis.
    /// </summary>
    public class UPhoneme {
        public bool AutoEnvelope = true;
        public bool AutoRemapped = true;
        public int DurTick;
        public EnvelopeExpression Envelope;
        public UOto Oto;
        public double Overlap;
        public bool OverlapCorrection = true;
        public bool Overlapped = false;
        public UNote Parent;

        public bool PhonemeError = false;
        public string PhonemeString = "a";

        public double PreUtter;
        public string RemappedBank = string.Empty;
        public double TailIntrude;
        public double TailOverlap;

        public UPhoneme() { Envelope = new EnvelopeExpression(this.Parent) {ParentPhoneme = this}; }
        public string PhonemeRemapped { get { return AutoRemapped ? PhonemeString + RemappedBank : PhonemeString; } }

        public UPhoneme Clone(UNote newParent) {
            var p = new UPhoneme {Parent = newParent};
            return p;
        }
    }
}
